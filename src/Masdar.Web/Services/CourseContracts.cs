using Masdar.Web.Domain.Enums;

namespace Masdar.Web.Services;

public sealed record CourseQuery(
    string? Search = null,
    int? UniversityId = null,
    int? MajorId = null,
    int Page = 1,
    int PageSize = 20);

public sealed record CourseListItem(
    int Id,
    string Code,
    string Name,
    string Instructor,
    string Semester,
    DateTime? ExamDateUtc,
    int MajorId,
    string MajorName,
    int UniversityId,
    string UniversityName,
    int PublishedResourceCount);

public sealed record RoadmapNodeItem(
    int Id,
    int Order,
    string Title,
    string Description,
    RoadmapDifficulty Difficulty,
    int ExamWeightPercent,
    bool IsCompleted);

public sealed record CourseWorkspace(
    CourseListItem Course,
    IReadOnlyList<ResourceListItem> TopResources,
    IReadOnlyList<RoadmapNodeItem> Roadmap,
    int ReadinessPercent);

public sealed record DashboardSummary(
    CourseListItem? ActiveCourse,
    int SelectedCourseCount,
    int SavedResourceCount,
    int CompletedRoadmapNodeCount,
    int TotalRoadmapNodeCount,
    int ReputationBalance,
    IReadOnlyList<ResourceListItem> RecommendedResources,
    IReadOnlyList<OpportunityItem> UpcomingOpportunities);

public interface ICourseService
{
    Task<PagedResult<CourseListItem>> SearchAsync(CourseQuery query, CancellationToken cancellationToken = default);
    Task<CourseWorkspace?> GetWorkspaceAsync(int courseId, string? userId, CancellationToken cancellationToken = default);
    Task<CourseListItem?> GetActiveCourseAsync(string userId, CancellationToken cancellationToken = default);
    Task<DashboardSummary> GetDashboardAsync(string userId, CancellationToken cancellationToken = default);
    Task SetActiveCourseAsync(int courseId, string userId, CancellationToken cancellationToken = default);
    Task<bool> ToggleRoadmapProgressAsync(int roadmapNodeId, string userId, CancellationToken cancellationToken = default);
}
