using Masdar.Web.Domain.Enums;

namespace Masdar.Web.Domain.Entities;

public sealed class StudyResource
{
    public long Id { get; set; }
    public int CourseId { get; set; }
    public string UploaderId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public ResourceType Type { get; set; }
    public string Semester { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? TutorContext { get; set; }
    public ResourcePublicationStatus PublicationStatus { get; set; } = ResourcePublicationStatus.Published;
    public ResourceVerificationStatus VerificationStatus { get; set; } = ResourceVerificationStatus.Unverified;
    public bool IsRepresentative { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string Sha256 { get; set; } = string.Empty;
    public FileScanStatus FileScanStatus { get; set; } = FileScanStatus.Validated;
    public string? ExternalUrl { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAtUtc { get; set; }
    public DateTime? ArchivedAtUtc { get; set; }

    public Course Course { get; set; } = null!;
    public ApplicationUser Uploader { get; set; } = null!;
    public ICollection<ResourceTag> ResourceTags { get; set; } = [];
    public ICollection<ResourceRating> Ratings { get; set; } = [];
    public ICollection<ResourceHelpfulVote> HelpfulVotes { get; set; } = [];
    public ICollection<SavedResource> SavedByUsers { get; set; } = [];
    public ICollection<ResourceRequest> FulfilledRequests { get; set; } = [];
}

public sealed class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<ResourceTag> ResourceTags { get; set; } = [];
}

public sealed class ResourceTag
{
    public long ResourceId { get; set; }
    public int TagId { get; set; }
    public StudyResource Resource { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}

public sealed class ResourceRating
{
    public long Id { get; set; }
    public long ResourceId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int Value { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public StudyResource Resource { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}

public sealed class ResourceHelpfulVote
{
    public long Id { get; set; }
    public long ResourceId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public StudyResource Resource { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}

public sealed class SavedResource
{
    public string UserId { get; set; } = string.Empty;
    public long ResourceId { get; set; }
    public DateTime SavedAtUtc { get; set; } = DateTime.UtcNow;
    public ApplicationUser User { get; set; } = null!;
    public StudyResource Resource { get; set; } = null!;
}
