using System.Buffers;
using System.IO.Compression;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Masdar.Web.Services;

public sealed class LocalFileStorageService : IFileStorageService
{
    private const string PdfContentType = "application/pdf";
    private const string DocxContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
    private static readonly IReadOnlyDictionary<string, string> AllowedTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = PdfContentType,
        [".docx"] = DocxContentType
    };

    private readonly string _rootPath;
    private readonly long _maximumBytes;

    public LocalFileStorageService(IWebHostEnvironment environment, IOptions<FileStorageOptions> options)
    {
        var configured = options.Value.RootPath;
        if (string.IsNullOrWhiteSpace(configured))
        {
            configured = "App_Data/uploads";
        }

        _rootPath = Path.GetFullPath(Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(environment.ContentRootPath, configured));
        _maximumBytes = options.Value.MaximumBytes is > 0 and <= 10 * 1024 * 1024
            ? options.Value.MaximumBytes
            : 10 * 1024 * 1024;
    }

    public async Task<StoredFile> SaveValidatedAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);
        if (file.Length <= 0)
        {
            throw new DomainValidationException("Choose a non-empty PDF or DOCX file.");
        }

        if (file.Length > _maximumBytes)
        {
            throw new DomainValidationException($"The file cannot exceed {_maximumBytes / (1024 * 1024)} MB.");
        }

        var originalName = Path.GetFileName(file.FileName).Trim();
        if (string.IsNullOrWhiteSpace(originalName) || originalName.Length > 255)
        {
            throw new DomainValidationException("The uploaded file name is invalid.");
        }

        var extension = Path.GetExtension(originalName);
        if (!AllowedTypes.TryGetValue(extension, out var expectedContentType))
        {
            throw new DomainValidationException("Only PDF and DOCX files are supported.");
        }

        var declaredContentType = file.ContentType?.Split(';', 2)[0].Trim();
        if (!string.Equals(declaredContentType, expectedContentType, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainValidationException("The file content type does not match its extension.");
        }

        var now = DateTime.UtcNow;
        var storageKey = $"{now:yyyy}/{now:MM}/{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var finalPath = ResolvePath(storageKey);
        Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);
        var temporaryPath = finalPath + ".uploading";
        string sha256;
        long bytesWritten = 0;

        try
        {
            await using (var source = file.OpenReadStream())
            await using (var destination = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                81_920,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            using (var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
            {
                var buffer = ArrayPool<byte>.Shared.Rent(81_920);
                try
                {
                    int read;
                    while ((read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
                    {
                        bytesWritten += read;
                        if (bytesWritten > _maximumBytes)
                        {
                            throw new DomainValidationException($"The file cannot exceed {_maximumBytes / (1024 * 1024)} MB.");
                        }

                        hash.AppendData(buffer, 0, read);
                        await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }

                sha256 = Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
            }

            if (bytesWritten == 0)
            {
                throw new DomainValidationException("Choose a non-empty PDF or DOCX file.");
            }

            ValidateFileSignature(temporaryPath, extension);
            File.Move(temporaryPath, finalPath);
            return new StoredFile(storageKey, originalName, expectedContentType, bytesWritten, sha256);
        }
        catch
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }

            throw;
        }
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = ResolvePath(storageKey);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("The requested resource file was not found.");
        }

        Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81_920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = ResolvePath(storageKey);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    private string ResolvePath(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey) || Path.IsPathRooted(storageKey))
        {
            throw new DomainValidationException("The storage key is invalid.");
        }

        var candidate = Path.GetFullPath(Path.Combine(_rootPath, storageKey.Replace('/', Path.DirectorySeparatorChar)));
        var rootPrefix = _rootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!candidate.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainValidationException("The storage key is invalid.");
        }

        return candidate;
    }

    private static void ValidateFileSignature(string path, string extension)
    {
        if (extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            Span<byte> signature = stackalloc byte[5];
            using var stream = File.OpenRead(path);
            if (stream.Read(signature) != signature.Length || !signature.SequenceEqual("%PDF-"u8))
            {
                throw new DomainValidationException("The uploaded file is not a valid PDF document.");
            }

            return;
        }

        try
        {
            using var archive = ZipFile.OpenRead(path);
            var hasContentTypes = archive.GetEntry("[Content_Types].xml") is not null;
            var hasWordDocument = archive.GetEntry("word/document.xml") is not null;
            if (!hasContentTypes || !hasWordDocument)
            {
                throw new DomainValidationException("The uploaded file is not a valid DOCX document.");
            }
        }
        catch (InvalidDataException)
        {
            throw new DomainValidationException("The uploaded file is not a valid DOCX document.");
        }
    }
}
