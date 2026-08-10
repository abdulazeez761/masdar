using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Masdar.Web.ViewModels;

public sealed class HomeDashboardViewModel
{
    public string DisplayName { get; init; } = "Student";
    public bool IsAuthenticated { get; init; }
    public CourseSummaryViewModel? ActiveCourse { get; init; }
    public IReadOnlyList<CourseSummaryViewModel> Courses { get; init; } = [];
    public IReadOnlyList<ResourceCardViewModel> RecommendedResources { get; init; } = [];
    public IReadOnlyList<RoadmapNodeViewModel> Roadmap { get; init; } = [];
    public IReadOnlyList<OpportunityViewModel> Opportunities { get; init; } = [];
    public int CompletedRoadmapNodes { get; init; }
    public int SavedResources { get; init; }
    public int ReputationPoints { get; init; }
}

public sealed class CourseCatalogViewModel
{
    public string? Search { get; init; }
    public int? UniversityId { get; init; }
    public int? MajorId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 12;
    public int TotalCount { get; init; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
    public IReadOnlyList<SelectListItem> Universities { get; init; } = [];
    public IReadOnlyList<SelectListItem> Majors { get; init; } = [];
    public IReadOnlyList<CourseSummaryViewModel> Courses { get; init; } = [];
}

public sealed class CourseSummaryViewModel
{
    public int Id { get; init; }
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public string University { get; init; } = "";
    public string Major { get; init; } = "";
    public string? Instructor { get; init; }
    public string? Semester { get; init; }
    public DateTime? ExamDate { get; init; }
    public int ResourceCount { get; init; }
    public bool IsActive { get; init; }
    public int ProgressPercent { get; init; }
}

public sealed class CourseDetailsViewModel
{
    public CourseSummaryViewModel Course { get; init; } = new();
    public string Tab { get; init; } = "overview";
    public IReadOnlyList<ResourceCardViewModel> Resources { get; init; } = [];
    public IReadOnlyList<RoadmapNodeViewModel> Roadmap { get; init; } = [];
    public IReadOnlyList<CareerConnectionViewModel> CareerConnections { get; init; } = [];
    public IReadOnlyList<OpportunityViewModel> Opportunities { get; init; } = [];
}

public sealed class RoadmapNodeViewModel
{
    public int Id { get; init; }
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public string Difficulty { get; init; } = "";
    public int WeightPercent { get; init; }
    public int SortOrder { get; init; }
    public bool IsCompleted { get; init; }
}

public sealed class CareerConnectionViewModel
{
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
}

public sealed class ResourceSearchViewModel
{
    public string? Search { get; init; }
    public int? CourseId { get; init; }
    public string? Type { get; init; }
    public string? Semester { get; init; }
    public bool SavedOnly { get; init; }
    public string Sort { get; init; } = "HighestRated";
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 9;
    public int TotalCount { get; init; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
    public IReadOnlyList<SelectListItem> Courses { get; init; } = [];
    public IReadOnlyList<SelectListItem> Types { get; init; } = [];
    public IReadOnlyList<SelectListItem> Semesters { get; init; } = [];
    public IReadOnlyList<ResourceCardViewModel> Resources { get; init; } = [];
}

public sealed class ResourceCardViewModel
{
    public long Id { get; init; }
    public int CourseId { get; init; }
    public string CourseCode { get; init; } = "";
    public string Title { get; init; } = "";
    public string Type { get; init; } = "";
    public string Semester { get; init; } = "";
    public string Description { get; init; } = "";
    public string UploaderName { get; init; } = "";
    public double AverageRating { get; init; }
    public int RatingCount { get; init; }
    public int HelpfulCount { get; init; }
    public bool IsVerified { get; init; }
    public bool IsRepresentative { get; init; }
    public bool IsSaved { get; init; }
    public bool IsHelpful { get; init; }
    public int? UserRating { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
}

public sealed class ResourceDetailsViewModel
{
    public ResourceCardViewModel Resource { get; init; } = new();
    public string? TutorContext { get; init; }
    public string OriginalFileName { get; init; } = "resource";
    public string ContentType { get; init; } = "application/octet-stream";
    public long FileSizeBytes { get; init; }
    public DateTime PublishedAtUtc { get; init; }
}

public sealed class ContributionCreateViewModel
{
    [Required(ErrorMessage = "Choose a course.")]
    [Display(Name = "Course")]
    public int? CourseId { get; set; }

    [Required(ErrorMessage = "Choose a resource type.")]
    [Display(Name = "Resource type")]
    public string? Type { get; set; }

    [Required, StringLength(160, MinimumLength = 4)]
    public string Title { get; set; } = "";

    [Required, StringLength(80)]
    public string Semester { get; set; } = "";

    [Required, StringLength(2000, MinimumLength = 20)]
    public string Description { get; set; } = "";

    [Display(Name = "Tags")]
    [StringLength(300)]
    public string? Tags { get; set; }

    [Display(Name = "Tutor context (optional)")]
    [StringLength(8000)]
    public string? TutorContext { get; set; }

    [Required(ErrorMessage = "Choose a PDF or DOCX file.")]
    public IFormFile? File { get; set; }

    [Range(typeof(bool), "true", "true", ErrorMessage = "You must confirm that you can share this material.")]
    public bool HasSharingConsent { get; set; }

    public long? RequestId { get; set; }
    public IReadOnlyList<SelectListItem> Courses { get; set; } = [];
    public IReadOnlyList<SelectListItem> Types { get; set; } = [];
}

public sealed class CommunityIndexViewModel
{
    public string? Search { get; init; }
    public int? CourseId { get; init; }
    public string? Type { get; init; }
    public string Tab { get; init; } = "requests";
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public int TotalCount { get; init; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
    public IReadOnlyList<SelectListItem> Courses { get; init; } = [];
    public IReadOnlyList<SelectListItem> Types { get; init; } = [];
    public IReadOnlyList<ResourceRequestViewModel> Requests { get; init; } = [];
    public IReadOnlyList<LeaderboardEntryViewModel> Leaders { get; init; } = [];
}

public sealed class ResourceRequestViewModel
{
    public long Id { get; init; }
    public int CourseId { get; init; }
    public string CourseCode { get; init; } = "";
    public string Type { get; init; } = "";
    public string Title { get; init; } = "";
    public string? Notes { get; init; }
    public string RequesterName { get; init; } = "";
    public int VoteCount { get; init; }
    public int BountyPoints { get; init; }
    public string Status { get; init; } = "Open";
    public bool HasVoted { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}

public sealed class LeaderboardEntryViewModel
{
    public int Rank { get; init; }
    public string UserId { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string? Department { get; init; }
    public int ResourceCount { get; init; }
    public int Points { get; init; }
    public bool IsCurrentUser { get; init; }
}

public sealed class CreateResourceRequestViewModel
{
    [Required]
    [Display(Name = "Course")]
    public int? CourseId { get; set; }

    [Required]
    [Display(Name = "Resource type")]
    public string? Type { get; set; }

    [Required, StringLength(160, MinimumLength = 4)]
    [Display(Name = "What material do you need?")]
    public string Title { get; set; } = "";

    [StringLength(1000)]
    public string? Notes { get; set; }

    [Range(10, 500)]
    [Display(Name = "Bounty points")]
    public int BountyPoints { get; set; } = 40;

    public IReadOnlyList<SelectListItem> Courses { get; set; } = [];
    public IReadOnlyList<SelectListItem> Types { get; set; } = [];
}

public sealed class OpportunityViewModel
{
    public long Id { get; init; }
    public string Type { get; init; } = "";
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public string Location { get; init; } = "";
    public DateTime StartsAtUtc { get; init; }
    public DateTime? RegistrationDeadlineUtc { get; init; }
    public string? Url { get; init; }
    public bool IsRegistered { get; init; }
    public int RegistrationCount { get; init; }
}

public sealed class ProfileViewModel
{
    public string DisplayName { get; init; } = "";
    public string Email { get; init; } = "";
    public string? Bio { get; init; }
    public string? University { get; init; }
    public string? Major { get; init; }
    public string? Department { get; init; }
    public int ReputationPoints { get; init; }
    public int UploadedResourceCount { get; init; }
    public int SavedResourceCount { get; init; }
    public int CompletedRoadmapCount { get; init; }
    public IReadOnlyList<ResourceCardViewModel> RecentUploads { get; init; } = [];
}

public sealed class EditProfileViewModel
{
    [Required, StringLength(100, MinimumLength = 2)]
    [Display(Name = "Display name")]
    public string DisplayName { get; set; } = "";

    [StringLength(1000)]
    public string? Bio { get; set; }

    [Display(Name = "University")]
    public int? UniversityId { get; set; }

    [Display(Name = "Major")]
    public int? MajorId { get; set; }

    public IReadOnlyList<SelectListItem> Universities { get; set; } = [];
    public IReadOnlyList<SelectListItem> Majors { get; set; } = [];
}

public sealed class TutorViewModel
{
    [Required]
    public int? CourseId { get; set; }

    [Required, StringLength(1000, MinimumLength = 2)]
    public string Question { get; set; } = "";

    public string? CourseName { get; set; }
    public string? Answer { get; set; }
    public IReadOnlyList<TutorCitationViewModel> Citations { get; set; } = [];
    public IReadOnlyList<SelectListItem> Courses { get; set; } = [];
    public IReadOnlyList<string> SuggestedQuestions { get; set; } = [];
}

public sealed class TutorCitationViewModel
{
    public long ResourceId { get; init; }
    public string Title { get; init; } = "";
    public string Excerpt { get; init; } = "";
}
