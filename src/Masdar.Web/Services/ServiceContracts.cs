using Masdar.Web.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Masdar.Web.Services;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}

public sealed class DomainValidationException(string message) : InvalidOperationException(message);

public sealed record ResourceQuery(
    string? Search = null,
    int? UniversityId = null,
    int? MajorId = null,
    IReadOnlyCollection<int>? CourseIds = null,
    ResourceType? Type = null,
    string? Semester = null,
    bool SavedOnly = false,
    ResourceSort Sort = ResourceSort.Quality,
    int Page = 1,
    int PageSize = 12);

public sealed record ResourceListItem(
    long Id,
    int CourseId,
    string CourseCode,
    string CourseName,
    string Title,
    ResourceType Type,
    string Semester,
    string UploaderName,
    double AverageRating,
    int RatingCount,
    int HelpfulCount,
    bool IsVerified,
    bool IsRepresentative,
    bool IsSaved,
    IReadOnlyList<string> Tags,
    DateTime PublishedAtUtc);

public sealed record ResourceDetails(
    long Id,
    int CourseId,
    string CourseCode,
    string CourseName,
    string Title,
    ResourceType Type,
    string Semester,
    string Description,
    string? TutorContext,
    string UploaderId,
    string UploaderName,
    double AverageRating,
    int RatingCount,
    int HelpfulCount,
    int? CurrentUserRating,
    bool IsHelpful,
    bool IsSaved,
    bool IsVerified,
    bool IsRepresentative,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes,
    IReadOnlyList<string> Tags,
    DateTime PublishedAtUtc);

public sealed record CreateResourceCommand(
    int CourseId,
    string Title,
    ResourceType Type,
    string Semester,
    string Description,
    string? TutorContext,
    IReadOnlyCollection<string>? Tags,
    IFormFile File,
    bool HasSharingPermission);

public sealed record ResourceInteractionResult(bool IsActive, int Count);
public sealed record ResourceRatingResult(int UserRating, double AverageRating, int RatingCount);
public sealed record ResourceDownload(Stream Content, string ContentType, string FileName);

public interface IResourceService
{
    Task<PagedResult<ResourceListItem>> SearchAsync(ResourceQuery query, string? userId, CancellationToken cancellationToken = default);
    Task<ResourceDetails?> GetDetailsAsync(long resourceId, string? userId, CancellationToken cancellationToken = default);
    Task<ResourceDownload?> OpenDownloadAsync(long resourceId, CancellationToken cancellationToken = default);
    Task<ResourceInteractionResult> ToggleSaveAsync(long resourceId, string userId, CancellationToken cancellationToken = default);
    Task<ResourceInteractionResult> ToggleHelpfulAsync(long resourceId, string userId, CancellationToken cancellationToken = default);
    Task<ResourceRatingResult> RateAsync(long resourceId, string userId, int value, CancellationToken cancellationToken = default);
    Task<long> CreateAsync(CreateResourceCommand command, string uploaderId, CancellationToken cancellationToken = default);
}

public sealed record StoredFile(
    string StorageKey,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    string Sha256);

public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";
    public string RootPath { get; set; } = "App_Data/uploads";
    public long MaximumBytes { get; set; } = 10 * 1024 * 1024;
}

public interface IFileStorageService
{
    Task<StoredFile> SaveValidatedAsync(IFormFile file, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}

public sealed record AuditEntry(
    string Action,
    string EntityType,
    string? EntityId = null,
    string? ActorUserId = null,
    string? Description = null,
    object? Metadata = null);

public interface IAuditService
{
    Task WriteAsync(AuditEntry entry, CancellationToken cancellationToken = default);
}
