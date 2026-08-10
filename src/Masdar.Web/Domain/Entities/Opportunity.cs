using Masdar.Web.Domain.Enums;

namespace Masdar.Web.Domain.Entities;

public sealed class Opportunity
{
    public long Id { get; set; }
    public OpportunityType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? RegistrationUrl { get; set; }
    public DateTime StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }
    public DateTime? RegistrationDeadlineUtc { get; set; }
    public int? Capacity { get; set; }
    public bool IsPublished { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<OpportunityRegistration> Registrations { get; set; } = [];
}

public sealed class OpportunityRegistration
{
    public long Id { get; set; }
    public long OpportunityId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public OpportunityRegistrationStatus Status { get; set; } = OpportunityRegistrationStatus.Registered;
    public DateTime RegisteredAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public Opportunity Opportunity { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}
