using Microsoft.AspNetCore.Identity;

namespace Masdar.Web.Domain.Entities;

public sealed class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int ReputationBalance { get; set; }
    public int? UniversityId { get; set; }
    public int? MajorId { get; set; }
    public string? ProfileImageKey { get; set; }
    public string? Bio { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAtUtc { get; set; }

    public University? University { get; set; }
    public Major? Major { get; set; }
    public ICollection<UserCourse> UserCourses { get; set; } = [];
    public ICollection<StudyResource> UploadedResources { get; set; } = [];
    public ICollection<ResourceRating> ResourceRatings { get; set; } = [];
    public ICollection<ResourceHelpfulVote> HelpfulVotes { get; set; } = [];
    public ICollection<SavedResource> SavedResources { get; set; } = [];
    public ICollection<UserRoadmapProgress> RoadmapProgress { get; set; } = [];
    public ICollection<ResourceRequest> ResourceRequests { get; set; } = [];
    public ICollection<ResourceRequestVote> ResourceRequestVotes { get; set; } = [];
    public ICollection<OpportunityRegistration> OpportunityRegistrations { get; set; } = [];
    public ICollection<ReputationTransaction> ReputationTransactions { get; set; } = [];
}
