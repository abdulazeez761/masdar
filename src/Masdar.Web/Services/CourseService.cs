using Masdar.Web.Data;
using Masdar.Web.Domain.Entities;
using Masdar.Web.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Masdar.Web.Services;

public sealed class CourseService(
    ApplicationDbContext dbContext,
    IResourceService resourceService,
    IOpportunityService opportunityService) : ICourseService
{
    public async Task<PagedResult<CourseListItem>> SearchAsync(
        CourseQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 60);
        var courses = ActiveCourses();

        if (query.UniversityId is not null)
        {
            courses = courses.Where(x => x.Major.UniversityId == query.UniversityId);
        }

        if (query.MajorId is not null)
        {
            courses = courses.Where(x => x.MajorId == query.MajorId);
        }

        var search = query.Search?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            if (search.Length > 100)
            {
                throw new DomainValidationException("Search text cannot exceed 100 characters.");
            }

            courses = courses.Where(x =>
                x.Code.Contains(search) ||
                x.Name.Contains(search) ||
                x.Instructor.Contains(search) ||
                x.Major.Name.Contains(search) ||
                x.Major.University.Name.Contains(search));
        }

        var total = await courses.CountAsync(cancellationToken);
        var items = await ProjectCourse(courses
            .OrderBy(x => x.Code)
            .ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize))
            .ToListAsync(cancellationToken);
        return new PagedResult<CourseListItem>(items, page, pageSize, total);
    }

    public async Task<CourseWorkspace?> GetWorkspaceAsync(
        int courseId,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        var course = await ProjectCourse(ActiveCourses().Where(x => x.Id == courseId))
            .SingleOrDefaultAsync(cancellationToken);
        if (course is null)
        {
            return null;
        }

        var roadmap = await dbContext.RoadmapNodes
            .AsNoTracking()
            .Where(x => x.CourseId == courseId && x.IsActive)
            .OrderBy(x => x.Order)
            .Select(x => new RoadmapNodeItem(
                x.Id,
                x.Order,
                x.Title,
                x.Description,
                x.Difficulty,
                x.ExamWeightPercent,
                userId != null && x.UserProgress.Any(progress => progress.UserId == userId && progress.IsCompleted)))
            .ToListAsync(cancellationToken);
        var weightTotal = roadmap.Sum(x => x.ExamWeightPercent);
        var completedWeight = roadmap.Where(x => x.IsCompleted).Sum(x => x.ExamWeightPercent);
        var readiness = weightTotal == 0 ? 0 : (int)Math.Round(completedWeight * 100d / weightTotal);
        var topResources = await resourceService.SearchAsync(
            new ResourceQuery(CourseIds: [courseId], Sort: ResourceSort.Quality, PageSize: 5),
            userId,
            cancellationToken);

        return new CourseWorkspace(course, topResources.Items, roadmap, readiness);
    }

    public async Task<DashboardSummary> GetDashboardAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        userId = RequireUserId(userId);
        var user = await dbContext.Users.AsNoTracking()
            .Where(x => x.Id == userId && x.IsActive)
            .Select(x => new { x.ReputationBalance })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new UnauthorizedAccessException("The account is unavailable.");

        var selectedCourseIds = await dbContext.UserCourses.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.IsActive)
            .ThenByDescending(x => x.LastAccessedAtUtc)
            .Select(x => x.CourseId)
            .ToListAsync(cancellationToken);
        var activeCourse = await GetActiveCourseAsync(userId, cancellationToken);
        var savedCount = await dbContext.SavedResources.CountAsync(x => x.UserId == userId, cancellationToken);
        var progressQuery = dbContext.RoadmapNodes
            .AsNoTracking()
            .Where(x => x.IsActive && selectedCourseIds.Contains(x.CourseId));

        // SQL Server cannot translate Count(predicate-with-Any) because that becomes an
        // aggregate over a correlated subquery. Keep these as two scalar queries instead.
        var totalRoadmapNodes = selectedCourseIds.Count == 0
            ? 0
            : await progressQuery.CountAsync(cancellationToken);
        var completedRoadmapNodes = selectedCourseIds.Count == 0
            ? 0
            : await progressQuery.CountAsync(
                node => node.UserProgress.Any(progress =>
                    progress.UserId == userId && progress.IsCompleted),
                cancellationToken);
        var recommended = selectedCourseIds.Count == 0
            ? new PagedResult<ResourceListItem>([], 1, 6, 0)
            : await resourceService.SearchAsync(
                new ResourceQuery(CourseIds: selectedCourseIds, Sort: ResourceSort.Quality, PageSize: 6),
                userId,
                cancellationToken);
        var opportunities = await opportunityService.ListAsync(userId, cancellationToken);

        return new DashboardSummary(
            activeCourse,
            selectedCourseIds.Count,
            savedCount,
            completedRoadmapNodes,
            totalRoadmapNodes,
            user.ReputationBalance,
            recommended.Items,
            opportunities.Take(4).ToList());
    }

    public async Task<CourseListItem?> GetActiveCourseAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        userId = RequireUserId(userId);

        // FirstOrDefault intentionally tolerates legacy data containing more than one
        // active row. The next SetActive call repairs that data inside one transaction.
        // Resolve the selection before projecting: ordering a positional record created
        // inside ProjectCourse is not translatable by EF Core's SQL Server provider.
        var activeCourseId = await dbContext.UserCourses
            .AsNoTracking()
            .Where(selection =>
                selection.UserId == userId &&
                selection.IsActive &&
                selection.User.IsActive &&
                selection.Course.IsActive &&
                selection.Course.Major.IsActive &&
                selection.Course.Major.University.IsActive)
            .OrderByDescending(selection => selection.LastAccessedAtUtc)
            .ThenByDescending(selection => selection.SelectedAtUtc)
            .ThenByDescending(selection => selection.CourseId)
            .Select(selection => (int?)selection.CourseId)
            .FirstOrDefaultAsync(cancellationToken);

        return activeCourseId is null
            ? null
            : await ProjectCourse(ActiveCourses().Where(course => course.Id == activeCourseId.Value))
                .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task SetActiveCourseAsync(
        int courseId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        userId = RequireUserId(userId);
        if (!await dbContext.Users.AnyAsync(
                user => user.Id == userId && user.IsActive,
                cancellationToken))
        {
            throw new UnauthorizedAccessException("The account is unavailable.");
        }

        if (!await ActiveCourses().AnyAsync(x => x.Id == courseId, cancellationToken))
        {
            throw new KeyNotFoundException("The course was not found.");
        }

        // Serializable isolation plus the UserId-leading indexes makes switching atomic
        // even when two browser tabs submit different courses at the same time. Loading
        // the rows into the change tracker also avoids mixing ExecuteUpdate with stale
        // tracked entities in the same request.
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        var selections = await dbContext.UserCourses
            .Where(x => x.UserId == userId)
            .ToListAsync(cancellationToken);
        foreach (var item in selections)
        {
            item.IsActive = false;
        }

        var selection = selections.FirstOrDefault(x => x.CourseId == courseId);
        var now = DateTime.UtcNow;
        if (selection is null)
        {
            dbContext.UserCourses.Add(new UserCourse
            {
                UserId = userId,
                CourseId = courseId,
                IsActive = true,
                SelectedAtUtc = now,
                LastAccessedAtUtc = now
            });
        }
        else
        {
            selection.IsActive = true;
            selection.LastAccessedAtUtc = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<bool> ToggleRoadmapProgressAsync(
        int roadmapNodeId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        userId = RequireUserId(userId);
        if (!await dbContext.RoadmapNodes.AnyAsync(x => x.Id == roadmapNodeId && x.IsActive && x.Course.IsActive, cancellationToken))
        {
            throw new KeyNotFoundException("The roadmap milestone was not found.");
        }

        var progress = await dbContext.UserRoadmapProgress.FindAsync([userId, roadmapNodeId], cancellationToken);
        var now = DateTime.UtcNow;
        if (progress is null)
        {
            progress = new UserRoadmapProgress
            {
                UserId = userId,
                RoadmapNodeId = roadmapNodeId,
                IsCompleted = true,
                CompletedAtUtc = now,
                UpdatedAtUtc = now
            };
            dbContext.UserRoadmapProgress.Add(progress);
        }
        else
        {
            progress.IsCompleted = !progress.IsCompleted;
            progress.CompletedAtUtc = progress.IsCompleted ? now : null;
            progress.UpdatedAtUtc = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return progress.IsCompleted;
    }

    private IQueryable<Course> ActiveCourses() => dbContext.Courses
        .AsNoTracking()
        .Where(x => x.IsActive && x.Major.IsActive && x.Major.University.IsActive);

    private static IQueryable<CourseListItem> ProjectCourse(IQueryable<Course> courses) => courses.Select(x => new CourseListItem(
        x.Id,
        x.Code,
        x.Name,
        x.Instructor,
        x.Semester,
        x.ExamDateUtc,
        x.MajorId,
        x.Major.Name,
        x.Major.UniversityId,
        x.Major.University.Name,
        x.Resources.Count(resource =>
            resource.PublicationStatus == ResourcePublicationStatus.Published &&
            resource.FileScanStatus == FileScanStatus.Validated)));

    private static string RequireUserId(string? userId) =>
        string.IsNullOrWhiteSpace(userId) ? throw new UnauthorizedAccessException("An authenticated user is required.") : userId;
}
