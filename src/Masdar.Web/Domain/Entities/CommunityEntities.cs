using Masdar.Web.Domain.Enums;

namespace Masdar.Web.Domain.Entities;

public sealed class RoadmapNode
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public int Order { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public RoadmapDifficulty Difficulty { get; set; }
    public int ExamWeightPercent { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Course Course { get; set; } = null!;
    public ICollection<UserRoadmapProgress> UserProgress { get; set; } = [];
}

public sealed class UserRoadmapProgress
{
    public string UserId { get; set; } = string.Empty;
    public int RoadmapNodeId { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public RoadmapNode RoadmapNode { get; set; } = null!;
}

public sealed class ResourceRequest
{
    public long Id { get; set; }
    public int CourseId { get; set; }
    public string RequesterId { get; set; } = string.Empty;
    public ResourceType DesiredResourceType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int BountyPoints { get; set; }
    public ResourceRequestStatus Status { get; set; } = ResourceRequestStatus.Open;
    public long? FulfilledByResourceId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? FulfilledAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public Course Course { get; set; } = null!;
    public ApplicationUser Requester { get; set; } = null!;
    public StudyResource? FulfilledByResource { get; set; }
    public ICollection<ResourceRequestVote> Votes { get; set; } = [];
}

public sealed class ResourceRequestVote
{
    public long Id { get; set; }
    public long ResourceRequestId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public ResourceRequest ResourceRequest { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}

public sealed class ReputationTransaction
{
    public long Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ReputationTransactionType Type { get; set; }
    public int Points { get; set; }
    public int BalanceAfter { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string? RelatedEntityType { get; set; }
    public string? RelatedEntityId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public ApplicationUser User { get; set; } = null!;
}
