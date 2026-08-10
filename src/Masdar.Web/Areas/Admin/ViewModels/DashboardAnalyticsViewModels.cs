namespace Masdar.Web.Areas.Admin.ViewModels;

public sealed class DashboardAnalyticsViewModel
{
    public DateTime GeneratedAtUtc { get; init; }
    public IReadOnlyList<DashboardKpiViewModel> Kpis { get; init; } = [];

    public int TotalUsers { get; init; }
    public int ActiveUsers { get; init; }
    public int TotalResources { get; init; }
    public int PublishedResources { get; init; }
    public int PendingResources { get; init; }
    public int TotalRequests { get; init; }
    public int OpenRequests { get; init; }
    public int FulfilledRequests { get; init; }
    public int TotalOpportunities { get; init; }
    public int UpcomingOpportunities { get; init; }
    public int ActiveRegistrations { get; init; }
    public int ActiveCourses { get; init; }
    public int ActiveUniversities { get; init; }

    public int ModerationQueueCount { get; init; }
    public int VerificationCoveragePercent { get; init; }
    public int ScanReadyPercent { get; init; }
    public double AverageRating { get; init; }
    public int RatingCount { get; init; }
    public int HelpfulVoteCount { get; init; }
    public int RepresentativeResourceCount { get; init; }

    public IReadOnlyList<DashboardModerationItemViewModel> ModerationQueue { get; init; } = [];
    public IReadOnlyList<DashboardResourceTypeViewModel> ResourceTypes { get; init; } = [];
    public IReadOnlyList<DashboardTopCourseViewModel> TopCourses { get; init; } = [];
    public IReadOnlyList<DashboardContributorViewModel> TopContributors { get; init; } = [];
    public IReadOnlyList<DashboardRecentResourceViewModel> RecentResources { get; init; } = [];
    public IReadOnlyList<DashboardRegistrationViewModel> RecentRegistrations { get; init; } = [];
    public IReadOnlyList<DashboardActivityViewModel> RecentActivity { get; init; } = [];
}

public sealed class DashboardKpiViewModel
{
    public string Label { get; init; } = string.Empty;
    public int Value { get; init; }
    public string SupportingText { get; init; } = string.Empty;
    public int Delta7Days { get; init; }
    public int Delta30Days { get; init; }
    public string DeltaDescription { get; init; } = "added";
    public string IconId { get; init; } = "icon-info";
    public string Tone { get; init; } = "blue";
}

public sealed class DashboardModerationItemViewModel
{
    public long Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string CourseCode { get; init; } = string.Empty;
    public string UploaderName { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public string PublicationStatus { get; init; } = string.Empty;
    public string ScanStatus { get; init; } = string.Empty;
    public string Tone { get; init; } = "amber";
}

public sealed class DashboardResourceTypeViewModel
{
    public string Label { get; init; } = string.Empty;
    public int Count { get; init; }
    public double SharePercent { get; init; }
    public int BarPercent { get; init; }
    public string Tone { get; init; } = "blue";
}

public sealed class DashboardTopCourseViewModel
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int PublishedResourceCount { get; init; }
    public int OpenRequestCount { get; init; }
    public int StudentCount { get; init; }
    public double AverageRating { get; init; }
    public int VolumePercent { get; init; }
}

public sealed class DashboardContributorViewModel
{
    public string Id { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Initials { get; init; } = string.Empty;
    public int PublishedResourceCount { get; init; }
    public int HelpfulVoteCount { get; init; }
    public int ReputationBalance { get; init; }
    public double AverageRating { get; init; }
}

public sealed class DashboardRecentResourceViewModel
{
    public long Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string CourseCode { get; init; } = string.Empty;
    public string UploaderName { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string StatusTone { get; init; } = "gray";
    public DateTime CreatedAtUtc { get; init; }
}

public sealed class DashboardRegistrationViewModel
{
    public long OpportunityId { get; init; }
    public string OpportunityTitle { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string OpportunityType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string StatusTone { get; init; } = "blue";
    public DateTime RegisteredAtUtc { get; init; }
}

public sealed class DashboardActivityViewModel
{
    public long Id { get; init; }
    public string Action { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string ActorName { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public string Tone { get; init; } = "blue";
    public DateTime CreatedAtUtc { get; init; }
}
