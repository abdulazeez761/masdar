using System.ComponentModel.DataAnnotations;
using Masdar.Web.Domain.Entities;
using Masdar.Web.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Masdar.Web.Areas.Admin.ViewModels;

public sealed class AdminDashboardViewModel
{
    public int TotalUsers { get; init; }
    public int ActiveUsers { get; init; }
    public int Universities { get; init; }
    public int Courses { get; init; }
    public int PublishedResources { get; init; }
    public int PendingResources { get; init; }
    public int OpenRequests { get; init; }
    public int OpportunityRegistrations { get; init; }
    public IReadOnlyList<ActivityLog> RecentActivity { get; init; } = [];
    public IReadOnlyList<StudyResource> RecentResources { get; init; } = [];
    public IReadOnlyList<ApplicationUser> RecentUsers { get; init; } = [];
}

public sealed class AdminUserRowViewModel
{
    public required ApplicationUser User { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = [];
}

public sealed class UniversityInputModel
{
    public int Id { get; set; }

    [Required, StringLength(160)]
    public string Name { get; set; } = string.Empty;

    [StringLength(160)]
    public string? Location { get; set; }

    [Url, StringLength(500)]
    public string? WebsiteUrl { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed class MajorInputModel
{
    public int Id { get; set; }

    [Required]
    public int UniversityId { get; set; }

    [Required, StringLength(160)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

public sealed class CourseInputModel
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "University and major")]
    public int MajorId { get; set; }

    [Required, StringLength(32)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(180)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(160)]
    public string Instructor { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string Semester { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "Exam date")]
    public DateTime? ExamDateUtc { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
    public IReadOnlyList<SelectListItem> MajorOptions { get; set; } = [];
}

public sealed class ResourceModerationInputModel
{
    public long Id { get; set; }

    [Required]
    [Display(Name = "Publication status")]
    public ResourcePublicationStatus PublicationStatus { get; set; }

    [Required]
    [Display(Name = "Verification status")]
    public ResourceVerificationStatus VerificationStatus { get; set; }

    [Display(Name = "Representative of this course")]
    public bool IsRepresentative { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }
}

public sealed class OpportunityInputModel : IValidatableObject
{
    public long Id { get; set; }

    [Required]
    public OpportunityType Type { get; set; }

    [Required, StringLength(180)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [StringLength(180)]
    public string? Location { get; set; }

    [Url, StringLength(500)]
    [Display(Name = "Registration URL")]
    public string? RegistrationUrl { get; set; }

    [Required, DataType(DataType.DateTime)]
    [Display(Name = "Starts at")]
    public DateTime StartsAtUtc { get; set; } = DateTime.UtcNow.AddDays(14);

    [DataType(DataType.DateTime)]
    [Display(Name = "Ends at")]
    public DateTime? EndsAtUtc { get; set; }

    [DataType(DataType.DateTime)]
    [Display(Name = "Registration deadline")]
    public DateTime? RegistrationDeadlineUtc { get; set; }

    [Range(1, 100000)]
    public int? Capacity { get; set; }

    public bool IsPublished { get; set; } = true;
    public bool IsActive { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndsAtUtc is not null && EndsAtUtc < StartsAtUtc)
            yield return new ValidationResult("The end date cannot precede the start date.", [nameof(EndsAtUtc)]);

        if (RegistrationDeadlineUtc is not null && RegistrationDeadlineUtc > StartsAtUtc)
            yield return new ValidationResult("The registration deadline cannot be after the start date.", [nameof(RegistrationDeadlineUtc)]);
    }
}
