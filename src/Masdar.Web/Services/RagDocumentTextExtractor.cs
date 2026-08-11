using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Extensions.Options;
using UglyToad.PdfPig;

namespace Masdar.Web.Services;

public sealed partial class RagDocumentTextExtractor(
    IFileStorageService fileStorage,
    IOptions<GeminiRagOptions> options) : IRagDocumentTextExtractor
{
    public async Task<IReadOnlyList<RagTextChunk>> ExtractAsync(
        RagSourceDocument document,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        await using var stream = await fileStorage.OpenReadAsync(document.StorageKey, cancellationToken);
        var sections = document.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
            ? ExtractPdf(stream, cancellationToken)
            : document.ContentType.Contains("wordprocessingml", StringComparison.OrdinalIgnoreCase)
                ? ExtractDocx(stream, cancellationToken)
                : [];

        var maximumCharacters = Math.Clamp(options.Value.MaximumCharactersPerResource, 5_000, 500_000);
        var remaining = maximumCharacters;
        var chunks = new List<RagTextChunk>();
        var sequence = 0;

        foreach (var section in sections)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (remaining <= 0) break;

            var text = Normalize(section.Text);
            if (string.IsNullOrWhiteSpace(text)) continue;
            if (text.Length > remaining) text = text[..remaining];
            remaining -= text.Length;

            foreach (var part in Chunk(text))
            {
                chunks.Add(new RagTextChunk(
                    document.ResourceId,
                    document.Title,
                    document.ResourceType,
                    part,
                    section.Location,
                    sequence++));
            }
        }

        // Metadata stays searchable and also supports legacy records whose uploaded file
        // is image-only or otherwise has no extractable text.
        var metadata = Normalize(string.Join("\n", new[]
        {
            document.Title,
            document.Description,
            document.TutorContext ?? string.Empty,
            string.Join(", ", document.Tags)
        }.Where(value => !string.IsNullOrWhiteSpace(value))));
        if (!string.IsNullOrWhiteSpace(metadata))
        {
            chunks.Add(new RagTextChunk(
                document.ResourceId,
                document.Title,
                document.ResourceType,
                metadata,
                "resource metadata",
                sequence));
        }

        return chunks;
    }

    private static IReadOnlyList<TextSection> ExtractPdf(Stream stream, CancellationToken cancellationToken)
    {
        var sections = new List<TextSection>();
        using var pdf = PdfDocument.Open(stream);
        foreach (var page in pdf.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();
            sections.Add(new TextSection(
                $"page {page.Number}",
                string.Join(' ', page.GetWords().Select(word => word.Text))));
        }

        return sections;
    }

    private static IReadOnlyList<TextSection> ExtractDocx(Stream stream, CancellationToken cancellationToken)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        var entry = archive.GetEntry("word/document.xml");
        if (entry is null) return [];

        using var documentStream = entry.Open();
        var document = XDocument.Load(documentStream, LoadOptions.None);
        XNamespace word = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        var sections = new List<TextSection>();
        var paragraphNumber = 0;
        foreach (var paragraph in document.Descendants(word + "p"))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var text = string.Concat(paragraph.Descendants(word + "t").Select(node => node.Value));
            if (string.IsNullOrWhiteSpace(text)) continue;
            paragraphNumber++;
            sections.Add(new TextSection($"paragraph {paragraphNumber}", text));
        }

        return sections;
    }

    private IEnumerable<string> Chunk(string text)
    {
        var chunkSize = Math.Clamp(options.Value.ChunkSize, 400, 3_000);
        var overlap = Math.Clamp(options.Value.ChunkOverlap, 0, chunkSize / 3);
        var start = 0;
        while (start < text.Length)
        {
            var end = Math.Min(start + chunkSize, text.Length);
            if (end < text.Length)
            {
                var boundary = text.LastIndexOf(' ', end - 1, end - start);
                if (boundary > start + (chunkSize / 2)) end = boundary;
            }

            var chunk = text[start..end].Trim();
            if (chunk.Length > 0) yield return chunk;
            if (end >= text.Length) yield break;

            start = Math.Max(start + 1, end - overlap);
            while (start < text.Length && char.IsWhiteSpace(text[start])) start++;
        }
    }

    private static string Normalize(string value) => WhitespacePattern().Replace(value, " ").Trim();

    private sealed record TextSection(string Location, string Text);

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespacePattern();
}
