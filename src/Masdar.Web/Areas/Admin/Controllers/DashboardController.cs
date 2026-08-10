using Masdar.Web.Areas.Admin.ViewModels;
using Masdar.Web.Data;
using Masdar.Web.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Masdar.Web.Areas.Admin.Controllers;

public sealed class DashboardController(ApplicationDbContext db) : AdminControllerBase(db)
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var sevenDaysAgo = now.AddDays(-7);
        var thirtyDaysAgo = now.AddDays(-30);

        var userStats = await Db.Users
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Total = group.Count(),
                Active = group.Count(user => user.IsActive),
                Added7Days = group.Count(user => user.CreatedAtUtc >= sevenDaysAgo),
                Added30Days = group.Count(user => user.CreatedAtUtc >= thirtyDaysAgo)
            })
            .SingleOrDefaultAsync(cancellationToken);

        var resourceStats = await Db.StudyResources
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Total = group.Count(),
                Published = group.Count(resource => resource.PublicationStatus == ResourcePublicationStatus.Published),
                Pending = group.Count(resource => resource.PublicationStatus == ResourcePublicationStatus.PendingReview),
                Verified = group.Count(resource =>
                    resource.PublicationStatus == ResourcePublicationStatus.Published &&
                    resource.VerificationStatus == ResourceVerificationStatus.Verified),
                ScanReady = group.Count(resource => resource.FileScanStatus == FileScanStatus.Validated),
                Representative = group.Count(resource =>
                    resource.PublicationStatus == ResourcePublicationStatus.Published && resource.IsRepresentative),
                Published7Days = group.Count(resource =>
                    resource.PublicationStatus == ResourcePublicationStatus.Published &&
                    resource.PublishedAtUtc >= sevenDaysAgo),
                Published30Days = group.Count(resource =>
                    resource.PublicationStatus == ResourcePublicationStatus.Published &&
                    resource.PublishedAtUtc >= thirtyDaysAgo)
            })
            .SingleOrDefaultAsync(cancellationToken);

        var requestStats = await Db.ResourceRequests
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Total = group.Count(),
                Open = group.Count(request => request.Status == ResourceRequestStatus.Open),
                Fulfilled = group.Count(request => request.Status == ResourceRequestStatus.Fulfilled),
                Added7Days = group.Count(request => request.CreatedAtUtc >= sevenDaysAgo),
                Added30Days = group.Count(request => request.CreatedAtUtc >= thirtyDaysAgo)
            })
            .SingleOrDefaultAsync(cancellationToken);

        var opportunityStats = await Db.Opportunities
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Total = group.Count(),
                Upcoming = group.Count(opportunity =>
                    opportunity.IsActive && opportunity.IsPublished && opportunity.StartsAtUtc >= now)
            })
            .SingleOrDefaultAsync(cancellationToken);

        var registrationStats = await Db.OpportunityRegistrations
            .AsNoTracking()
            .Where(registration =>
                registration.Status == OpportunityRegistrationStatus.Registered ||
                registration.Status == OpportunityRegistrationStatus.Waitlisted)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Active = group.Count(),
                Added7Days = group.Count(registration => registration.RegisteredAtUtc >= sevenDaysAgo),
                Added30Days = group.Count(registration => registration.RegisteredAtUtc >= thirtyDaysAgo)
            })
            .SingleOrDefaultAsync(cancellationToken);

        var ratingStats = await Db.ResourceRatings
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Count = group.Count(),
                Average = group.Average(rating => (double)rating.Value)
            })
            .SingleOrDefaultAsync(cancellationToken);

        var activeUniversities = await Db.Universities
            .AsNoTracking()
            .CountAsync(university => university.IsActive, cancellationToken);
        var activeCourses = await Db.Courses
            .AsNoTracking()
            .CountAsync(course => course.IsActive, cancellationToken);
        var helpfulVoteCount = await Db.ResourceHelpfulVotes
            .AsNoTracking()
            .CountAsync(cancellationToken);

        var moderationQuery = Db.StudyResources
            .AsNoTracking()
            .Where(resource =>
                resource.PublicationStatus == ResourcePublicationStatus.PendingReview ||
                resource.FileScanStatus == FileScanStatus.Pending ||
                resource.FileScanStatus == FileScanStatus.Failed ||
                resource.FileScanStatus == FileScanStatus.Infected);

        var moderationQueueCount = await moderationQuery.CountAsync(cancellationToken);
        var moderationRaw = await moderationQuery
            .OrderBy(resource => resource.FileScanStatus == FileScanStatus.Infected ? 0 : 1)
            .ThenBy(resource => resource.CreatedAtUtc)
            .Select(resource => new
            {
                resource.Id,
                resource.Title,
                CourseCode = resource.Course.Code,
                UploaderName = resource.Uploader.DisplayName,
                resource.CreatedAtUtc,
                resource.PublicationStatus,
                resource.FileScanStatus
            })
            .Take(7)
            .ToListAsync(cancellationToken);

        var moderationQueue = moderationRaw
            .Select(resource => new DashboardModerationItemViewModel
            {
                Id = resource.Id,
                Title = resource.Title,
                CourseCode = resource.CourseCode,
                UploaderName = DisplayNameOrFallback(resource.UploaderName),
                CreatedAtUtc = resource.CreatedAtUtc,
                PublicationStatus = PublicationStatusLabel(resource.PublicationStatus),
                ScanStatus = FileScanStatusLabel(resource.FileScanStatus),
                Tone = resource.FileScanStatus is FileScanStatus.Infected or FileScanStatus.Failed ? "red" : "amber"
            })
            .ToList();

        var distributionRaw = await Db.StudyResources
            .AsNoTracking()
            .Where(resource => resource.PublicationStatus == ResourcePublicationStatus.Published)
            .GroupBy(resource => resource.Type)
            .Select(group => new { Type = group.Key, Count = group.Count() })
            .OrderByDescending(item => item.Count)
            .ToListAsync(cancellationToken);

        var distributionTotal = distributionRaw.Sum(item => item.Count);
        var distributionMaximum = distributionRaw.Count == 0 ? 0 : distributionRaw.Max(item => item.Count);
        var resourceTypes = distributionRaw
            .Select((item, index) => new DashboardResourceTypeViewModel
            {
                Label = ResourceTypeLabel(item.Type),
                Count = item.Count,
                SharePercent = Percentage(item.Count, distributionTotal, 1),
                BarPercent = Percentage(item.Count, distributionMaximum),
                Tone = DistributionTone(index)
            })
            .ToList();

        var topCourses = await Db.Courses
            .AsNoTracking()
            .Where(course => course.IsActive)
            .Select(course => new DashboardTopCourseViewModel
            {
                Id = course.Id,
                Code = course.Code,
                Name = course.Name,
                PublishedResourceCount = course.Resources.Count(resource =>
                    resource.PublicationStatus == ResourcePublicationStatus.Published),
                OpenRequestCount = course.ResourceRequests.Count(request => request.Status == ResourceRequestStatus.Open),
                StudentCount = course.UserCourses.Count(),
                AverageRating = course.Resources
                    .Where(resource => resource.PublicationStatus == ResourcePublicationStatus.Published)
                    .SelectMany(resource => resource.Ratings)
                    .Average(rating => (double?)rating.Value) ?? 0,
                VolumePercent = 0
            })
            .OrderByDescending(course => course.PublishedResourceCount)
            .ThenByDescending(course => course.StudentCount)
            .ThenBy(course => course.Code)
            .Take(6)
            .ToListAsync(cancellationToken);

        var topCourseMaximum = topCourses.Count == 0 ? 0 : topCourses.Max(course => course.PublishedResourceCount);
        topCourses = topCourses
            .Select(course => new DashboardTopCourseViewModel
            {
                Id = course.Id,
                Code = course.Code,
                Name = course.Name,
                PublishedResourceCount = course.PublishedResourceCount,
                OpenRequestCount = course.OpenRequestCount,
                StudentCount = course.StudentCount,
                AverageRating = course.AverageRating,
                VolumePercent = Percentage(course.PublishedResourceCount, topCourseMaximum)
            })
            .ToList();

        var contributorRaw = await Db.Users
            .AsNoTracking()
            .Where(user => user.UploadedResources.Any(resource =>
                resource.PublicationStatus == ResourcePublicationStatus.Published))
            .Select(user => new
            {
                user.Id,
                user.DisplayName,
                user.UserName,
                user.ReputationBalance,
                PublishedResourceCount = user.UploadedResources.Count(resource =>
                    resource.PublicationStatus == ResourcePublicationStatus.Published),
                HelpfulVoteCount = user.UploadedResources
                    .Where(resource => resource.PublicationStatus == ResourcePublicationStatus.Published)
                    .SelectMany(resource => resource.HelpfulVotes)
                    .Count(),
                AverageRating = user.UploadedResources
                    .Where(resource => resource.PublicationStatus == ResourcePublicationStatus.Published)
                    .SelectMany(resource => resource.Ratings)
                    .Average(rating => (double?)rating.Value) ?? 0
            })
            .OrderByDescending(user => user.PublishedResourceCount)
            .ThenByDescending(user => user.HelpfulVoteCount)
            .ThenByDescending(user => user.ReputationBalance)
            .Take(6)
            .ToListAsync(cancellationToken);

        var topContributors = contributorRaw
            .Select(user =>
            {
                var displayName = DisplayNameOrFallback(user.DisplayName, user.UserName);
                return new DashboardContributorViewModel
                {
                    Id = user.Id,
                    DisplayName = displayName,
                    Initials = Initials(displayName),
                    PublishedResourceCount = user.PublishedResourceCount,
                    HelpfulVoteCount = user.HelpfulVoteCount,
                    ReputationBalance = user.ReputationBalance,
                    AverageRating = user.AverageRating
                };
            })
            .ToList();

        var recentResourceRaw = await Db.StudyResources
            .AsNoTracking()
            .OrderByDescending(resource => resource.CreatedAtUtc)
            .Select(resource => new
            {
                resource.Id,
                resource.Title,
                CourseCode = resource.Course.Code,
                UploaderName = resource.Uploader.DisplayName,
                resource.Type,
                resource.PublicationStatus,
                resource.CreatedAtUtc
            })
            .Take(6)
            .ToListAsync(cancellationToken);

        var recentResources = recentResourceRaw
            .Select(resource => new DashboardRecentResourceViewModel
            {
                Id = resource.Id,
                Title = resource.Title,
                CourseCode = resource.CourseCode,
                UploaderName = DisplayNameOrFallback(resource.UploaderName),
                Type = ResourceTypeLabel(resource.Type),
                Status = PublicationStatusLabel(resource.PublicationStatus),
                StatusTone = PublicationStatusTone(resource.PublicationStatus),
                CreatedAtUtc = resource.CreatedAtUtc
            })
            .ToList();

        var recentRegistrationRaw = await Db.OpportunityRegistrations
            .AsNoTracking()
            .OrderByDescending(registration => registration.RegisteredAtUtc)
            .Select(registration => new
            {
                registration.OpportunityId,
                OpportunityTitle = registration.Opportunity.Title,
                registration.Opportunity.Type,
                UserDisplayName = registration.User.DisplayName,
                registration.User.UserName,
                registration.Status,
                registration.RegisteredAtUtc
            })
            .Take(6)
            .ToListAsync(cancellationToken);

        var recentRegistrations = recentRegistrationRaw
            .Select(registration => new DashboardRegistrationViewModel
            {
                OpportunityId = registration.OpportunityId,
                OpportunityTitle = registration.OpportunityTitle,
                UserName = DisplayNameOrFallback(registration.UserDisplayName, registration.UserName),
                OpportunityType = OpportunityTypeLabel(registration.Type),
                Status = OpportunityRegistrationStatusLabel(registration.Status),
                StatusTone = OpportunityRegistrationStatusTone(registration.Status),
                RegisteredAtUtc = registration.RegisteredAtUtc
            })
            .ToList();

        var recentActivityRaw = await Db.ActivityLogs
            .AsNoTracking()
            .OrderByDescending(activity => activity.CreatedAtUtc)
            .Select(activity => new
            {
                activity.Id,
                activity.Action,
                activity.Description,
                activity.EntityType,
                ActorDisplayName = activity.ActorUser == null ? null : activity.ActorUser.DisplayName,
                ActorUserName = activity.ActorUser == null ? null : activity.ActorUser.UserName,
                activity.CreatedAtUtc
            })
            .Take(7)
            .ToListAsync(cancellationToken);

        var recentActivity = recentActivityRaw
            .Select(activity => new DashboardActivityViewModel
            {
                Id = activity.Id,
                Action = ActivityLabel(activity.Action),
                Description = string.IsNullOrWhiteSpace(activity.Description)
                    ? $"Updated {activity.EntityType}"
                    : activity.Description,
                ActorName = activity.ActorDisplayName is null && activity.ActorUserName is null
                    ? "System"
                    : DisplayNameOrFallback(activity.ActorDisplayName, activity.ActorUserName),
                EntityType = activity.EntityType,
                Tone = ActivityTone(activity.Action),
                CreatedAtUtc = activity.CreatedAtUtc
            })
            .ToList();

        var totalUsers = userStats?.Total ?? 0;
        var activeUsers = userStats?.Active ?? 0;
        var totalResources = resourceStats?.Total ?? 0;
        var publishedResources = resourceStats?.Published ?? 0;
        var pendingResources = resourceStats?.Pending ?? 0;
        var totalRequests = requestStats?.Total ?? 0;
        var openRequests = requestStats?.Open ?? 0;
        var fulfilledRequests = requestStats?.Fulfilled ?? 0;
        var totalOpportunities = opportunityStats?.Total ?? 0;
        var upcomingOpportunities = opportunityStats?.Upcoming ?? 0;
        var activeRegistrations = registrationStats?.Active ?? 0;

        var model = new DashboardAnalyticsViewModel
        {
            GeneratedAtUtc = now,
            TotalUsers = totalUsers,
            ActiveUsers = activeUsers,
            TotalResources = totalResources,
            PublishedResources = publishedResources,
            PendingResources = pendingResources,
            TotalRequests = totalRequests,
            OpenRequests = openRequests,
            FulfilledRequests = fulfilledRequests,
            TotalOpportunities = totalOpportunities,
            UpcomingOpportunities = upcomingOpportunities,
            ActiveRegistrations = activeRegistrations,
            ActiveCourses = activeCourses,
            ActiveUniversities = activeUniversities,
            ModerationQueueCount = moderationQueueCount,
            VerificationCoveragePercent = Percentage(resourceStats?.Verified ?? 0, publishedResources),
            ScanReadyPercent = Percentage(resourceStats?.ScanReady ?? 0, totalResources),
            AverageRating = ratingStats?.Average ?? 0,
            RatingCount = ratingStats?.Count ?? 0,
            HelpfulVoteCount = helpfulVoteCount,
            RepresentativeResourceCount = resourceStats?.Representative ?? 0,
            ModerationQueue = moderationQueue,
            ResourceTypes = resourceTypes,
            TopCourses = topCourses,
            TopContributors = topContributors,
            RecentResources = recentResources,
            RecentRegistrations = recentRegistrations,
            RecentActivity = recentActivity,
            Kpis =
            [
                new DashboardKpiViewModel
                {
                    Label = "Community members",
                    Value = totalUsers,
                    SupportingText = $"{activeUsers:N0} active · {Math.Max(0, totalUsers - activeUsers):N0} inactive",
                    Delta7Days = userStats?.Added7Days ?? 0,
                    Delta30Days = userStats?.Added30Days ?? 0,
                    DeltaDescription = "joined",
                    IconId = "icon-users",
                    Tone = "blue"
                },
                new DashboardKpiViewModel
                {
                    Label = "Published resources",
                    Value = publishedResources,
                    SupportingText = $"{pendingResources:N0} awaiting moderation",
                    Delta7Days = resourceStats?.Published7Days ?? 0,
                    Delta30Days = resourceStats?.Published30Days ?? 0,
                    DeltaDescription = "published",
                    IconId = "icon-library",
                    Tone = "green"
                },
                new DashboardKpiViewModel
                {
                    Label = "Resource requests",
                    Value = openRequests,
                    SupportingText = $"{fulfilledRequests:N0} fulfilled overall",
                    Delta7Days = requestStats?.Added7Days ?? 0,
                    Delta30Days = requestStats?.Added30Days ?? 0,
                    DeltaDescription = "created",
                    IconId = "icon-info",
                    Tone = "amber"
                },
                new DashboardKpiViewModel
                {
                    Label = "Active registrations",
                    Value = activeRegistrations,
                    SupportingText = $"{upcomingOpportunities:N0} upcoming opportunities",
                    Delta7Days = registrationStats?.Added7Days ?? 0,
                    Delta30Days = registrationStats?.Added30Days ?? 0,
                    DeltaDescription = "registered",
                    IconId = "icon-sparkles",
                    Tone = "cyan"
                }
            ]
        };

        return View(model);
    }

    private static int Percentage(int numerator, int denominator) =>
        denominator <= 0 ? 0 : (int)Math.Round(numerator * 100d / denominator);

    private static double Percentage(int numerator, int denominator, int decimalPlaces) =>
        denominator <= 0 ? 0 : Math.Round(numerator * 100d / denominator, decimalPlaces);

    private static string DisplayNameOrFallback(string? displayName, string? userName = null) =>
        !string.IsNullOrWhiteSpace(displayName)
            ? displayName
            : !string.IsNullOrWhiteSpace(userName)
                ? userName.Split('@')[0]
                : "MASDAR member";

    private static string Initials(string displayName)
    {
        var parts = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length switch
        {
            0 => "M",
            1 => parts[0][..1].ToUpperInvariant(),
            _ => string.Concat(parts[0][0], parts[^1][0]).ToUpperInvariant()
        };
    }

    private static string ResourceTypeLabel(ResourceType type) => type switch
    {
        ResourceType.PreviousExam => "Previous exams",
        ResourceType.Assignment => "Assignments",
        ResourceType.QuizBank => "Quiz banks",
        ResourceType.LectureSummary => "Lecture summaries",
        ResourceType.CheatSheet => "Cheat sheets",
        ResourceType.StudentNote => "Student notes",
        ResourceType.RecommendedVideo => "Recommended videos",
        ResourceType.ExternalResource => "External resources",
        _ => type.ToString()
    };

    private static string OpportunityTypeLabel(OpportunityType type) => type switch
    {
        OpportunityType.Hackathon => "Hackathon",
        OpportunityType.Competition => "Competition",
        OpportunityType.Workshop => "Workshop",
        OpportunityType.Internship => "Internship",
        OpportunityType.Meetup => "Meetup",
        OpportunityType.Bootcamp => "Bootcamp",
        OpportunityType.Scholarship => "Scholarship",
        OpportunityType.Volunteer => "Volunteer",
        OpportunityType.Talk => "Talk",
        _ => "Other"
    };

    private static string PublicationStatusLabel(ResourcePublicationStatus status) => status switch
    {
        ResourcePublicationStatus.PendingReview => "Pending review",
        ResourcePublicationStatus.Published => "Published",
        ResourcePublicationStatus.Rejected => "Rejected",
        ResourcePublicationStatus.Archived => "Archived",
        _ => "Draft"
    };

    private static string PublicationStatusTone(ResourcePublicationStatus status) => status switch
    {
        ResourcePublicationStatus.Published => "green",
        ResourcePublicationStatus.PendingReview => "amber",
        ResourcePublicationStatus.Rejected => "red",
        _ => "gray"
    };

    private static string FileScanStatusLabel(FileScanStatus status) => status switch
    {
        FileScanStatus.Validated => "Scan ready",
        FileScanStatus.Pending => "Scan pending",
        FileScanStatus.Infected => "Threat detected",
        FileScanStatus.Failed => "Scan failed",
        _ => status.ToString()
    };

    private static string OpportunityRegistrationStatusLabel(OpportunityRegistrationStatus status) => status switch
    {
        OpportunityRegistrationStatus.Registered => "Registered",
        OpportunityRegistrationStatus.Waitlisted => "Waitlisted",
        OpportunityRegistrationStatus.Cancelled => "Cancelled",
        OpportunityRegistrationStatus.Attended => "Attended",
        _ => status.ToString()
    };

    private static string OpportunityRegistrationStatusTone(OpportunityRegistrationStatus status) => status switch
    {
        OpportunityRegistrationStatus.Registered => "blue",
        OpportunityRegistrationStatus.Waitlisted => "amber",
        OpportunityRegistrationStatus.Attended => "green",
        _ => "gray"
    };

    private static string ActivityLabel(string action) => string.IsNullOrWhiteSpace(action)
        ? "Platform update"
        : string.Concat(action.Select((character, index) =>
            index > 0 && char.IsUpper(character) ? $" {char.ToLowerInvariant(character)}" : character.ToString()));

    private static string ActivityTone(string action)
    {
        if (action.Contains("Archive", StringComparison.OrdinalIgnoreCase) ||
            action.Contains("Deactivate", StringComparison.OrdinalIgnoreCase) ||
            action.Contains("Reject", StringComparison.OrdinalIgnoreCase))
            return "red";
        if (action.Contains("Publish", StringComparison.OrdinalIgnoreCase) ||
            action.Contains("Create", StringComparison.OrdinalIgnoreCase) ||
            action.Contains("Activate", StringComparison.OrdinalIgnoreCase))
            return "green";
        if (action.Contains("Moderate", StringComparison.OrdinalIgnoreCase) ||
            action.Contains("Role", StringComparison.OrdinalIgnoreCase))
            return "amber";
        return "blue";
    }

    private static string DistributionTone(int index) => (index % 5) switch
    {
        1 => "cyan",
        2 => "green",
        3 => "amber",
        4 => "violet",
        _ => "blue"
    };
}
