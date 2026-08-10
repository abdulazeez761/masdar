using Masdar.Web.Data;
using Masdar.Web.Domain.Entities;
using Masdar.Web.Domain.Enums;
using Masdar.Web.Services;
using Masdar.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Masdar.Web.Controllers;

public sealed class ResourcesController(
    IResourceService resourceService,
    ICourseService courseService,
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        int? universityId,
        int? majorId,
        int[]? courseIds,
        int? courseId,
        string? type,
        string? semester,
        string sort = "Quality",
        bool savedOnly = false,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var userId = userManager.GetUserId(User);
        if (userId is null)
        {
            savedOnly = false;
        }

        var courses = await dbContext.Courses.AsNoTracking()
            .Where(x => x.IsActive && x.Major.IsActive && x.Major.University.IsActive)
            .OrderBy(x => x.Code)
            .Select(x => new AcademicCourseRow(
                x.Id,
                x.Code,
                x.Name,
                x.Instructor,
                x.MajorId,
                x.Major.Name,
                x.Major.UniversityId,
                x.Major.University.Name))
            .ToListAsync(cancellationToken);

        var userContext = userId is null
            ? null
            : await dbContext.Users.AsNoTracking()
                .Where(x => x.Id == userId)
                .Select(x => new { x.UniversityId, x.MajorId })
                .SingleOrDefaultAsync(cancellationToken);
        var userCourseRows = userId is null
            ? []
            : await dbContext.UserCourses.AsNoTracking()
                .Where(x => x.UserId == userId && x.Course.IsActive)
                .OrderByDescending(x => x.IsActive)
                .ThenByDescending(x => x.LastAccessedAtUtc)
                .Select(x => new UserCourseRow(x.CourseId, x.IsActive))
                .ToListAsync(cancellationToken);
        var activeCourseId = userCourseRows.FirstOrDefault(x => x.IsActive)?.CourseId;

        var requestedIds = (courseIds ?? [])
            .Where(x => x > 0)
            .Append(courseId.GetValueOrDefault())
            .Where(x => x > 0)
            .Distinct()
            .Take(12)
            .ToArray();
        var firstRequested = courses.FirstOrDefault(x => requestedIds.Contains(x.Id));
        var firstActive = courses.FirstOrDefault(x => x.Id == activeCourseId);

        var universities = courses
            .Select(x => new AcademicOptionViewModel { Id = x.UniversityId, Name = x.UniversityName })
            .DistinctBy(x => x.Id)
            .OrderBy(x => x.Name)
            .ToArray();
        var resolvedUniversityId = universityId
            ?? firstRequested?.UniversityId
            ?? userContext?.UniversityId
            ?? firstActive?.UniversityId
            ?? universities.FirstOrDefault()?.Id
            ?? 0;
        if (!universities.Any(x => x.Id == resolvedUniversityId))
        {
            resolvedUniversityId = universities.FirstOrDefault()?.Id ?? 0;
        }

        var majors = courses
            .Where(x => x.UniversityId == resolvedUniversityId)
            .Select(x => new AcademicOptionViewModel { Id = x.MajorId, Name = x.MajorName })
            .DistinctBy(x => x.Id)
            .OrderBy(x => x.Name)
            .ToArray();
        var resolvedMajorId = majorId
            ?? firstRequested?.MajorId
            ?? (userContext?.UniversityId == resolvedUniversityId ? userContext.MajorId : null)
            ?? (firstActive?.UniversityId == resolvedUniversityId ? (int?)firstActive.MajorId : null)
            ?? majors.FirstOrDefault()?.Id
            ?? 0;
        if (!majors.Any(x => x.Id == resolvedMajorId))
        {
            resolvedMajorId = majors.FirstOrDefault()?.Id ?? 0;
        }

        var availableCourses = courses.Where(x => x.MajorId == resolvedMajorId).ToArray();
        var selectedIds = requestedIds.Where(id => availableCourses.Any(course => course.Id == id)).ToList();
        if (selectedIds.Count == 0)
        {
            selectedIds.AddRange(userCourseRows
                .Where(userCourse => availableCourses.Any(course => course.Id == userCourse.CourseId))
                .Select(x => x.CourseId)
                .Take(6));
        }
        if (selectedIds.Count == 0 && availableCourses.Length > 0)
        {
            selectedIds.Add(availableCourses[0].Id);
        }

        var selectedIdSet = selectedIds.ToHashSet();
        var resourceType = ParseResourceType(type);
        var resourceSort = ParseResourceSort(sort);
        var normalizedSort = resourceSort switch
        {
            ResourceSort.MostHelpful => "MostHelpful",
            ResourceSort.Newest => "Newest",
            ResourceSort.Title => "Title",
            _ => "Quality"
        };

        PagedResult<ResourceListItem> result;
        try
        {
            result = await resourceService.SearchAsync(new ResourceQuery(
                search,
                CourseIds: selectedIds.Count > 0 ? selectedIds : [int.MinValue],
                Type: resourceType,
                Semester: semester,
                SavedOnly: savedOnly,
                Sort: resourceSort,
                Page: Math.Max(1, page),
                PageSize: 12), userId, cancellationToken);
        }
        catch (DomainValidationException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
            result = new PagedResult<ResourceListItem>([], 1, 12, 0);
        }

        var semesterValues = await dbContext.StudyResources.AsNoTracking()
            .Where(x => x.PublicationStatus == ResourcePublicationStatus.Published &&
                        (selectedIds.Count == 0 || selectedIds.Contains(x.CourseId)))
            .Select(x => x.Semester)
            .Distinct()
            .OrderByDescending(x => x)
            .ToListAsync(cancellationToken);
        var courseOptions = availableCourses.Select(x => new ExplorerCourseOptionViewModel
        {
            Id = x.Id,
            Code = x.Code,
            Name = x.Name,
            Instructor = x.Instructor,
            IsSelected = selectedIdSet.Contains(x.Id),
            IsActive = x.Id == activeCourseId
        }).ToArray();
        var selectedCourses = courseOptions.Where(x => x.IsSelected).ToArray();

        var model = new ResourceExplorerViewModel
        {
            Search = search,
            UniversityId = resolvedUniversityId,
            MajorId = resolvedMajorId,
            SelectedCourseIds = selectedIds,
            ActiveCourseId = activeCourseId,
            Type = resourceType?.ToString(),
            Semester = semester,
            Sort = normalizedSort,
            SavedOnly = savedOnly,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount,
            Universities = universities,
            Majors = majors,
            AvailableCourses = courseOptions,
            SelectedCourses = selectedCourses,
            ResourceTypes = BuildResourceTypes(resourceType),
            Semesters = semesterValues,
            Resources = result.Items.Select(ToExplorerCard).ToArray()
        };
        ViewData["ActiveCourseCode"] = selectedCourses.FirstOrDefault(x => x.IsActive)?.Code ?? firstActive?.Code;
        ViewData["ActiveCourseName"] = selectedCourses.FirstOrDefault(x => x.IsActive)?.Name ?? firstActive?.Name;
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(long id, CancellationToken cancellationToken)
    {
        var details = await resourceService.GetDetailsAsync(id, userManager.GetUserId(User), cancellationToken);
        if (details is null)
        {
            return NotFound();
        }

        ViewData["ActiveCourseCode"] = details.CourseCode;
        ViewData["ActiveCourseName"] = details.CourseName;
        return View(new ResourceDetailsViewModel
        {
            Resource = details.ToViewModel(),
            TutorContext = details.TutorContext,
            OriginalFileName = details.OriginalFileName,
            ContentType = details.ContentType,
            FileSizeBytes = details.FileSizeBytes,
            PublishedAtUtc = details.PublishedAtUtc
        });
    }

    [HttpGet]
    public async Task<IActionResult> Download(long id, CancellationToken cancellationToken)
    {
        var download = await resourceService.OpenDownloadAsync(id, cancellationToken);
        if (download is null)
        {
            return NotFound();
        }

        Response.Headers.XContentTypeOptions = "nosniff";
        return File(download.Content, download.ContentType, download.FileName, enableRangeProcessing: true);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetActiveCourse(int id, string? returnUrl, CancellationToken cancellationToken)
    {
        try
        {
            await courseService.SetActiveCourseAsync(id, RequireUserId(), cancellationToken);
            TempData["SuccessMessage"] = "Your active course was updated for the workspace and tutor.";
        }
        catch (DomainValidationException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }

        return SafeReturn(returnUrl, nameof(Index));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> ToggleSave(long id, string? returnUrl, CancellationToken cancellationToken) =>
        RunInteractionAsync(id, returnUrl, () => resourceService.ToggleSaveAsync(id, RequireUserId(), cancellationToken), "Saved to your library.", "Removed from saved resources.");

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> ToggleHelpful(long id, string? returnUrl, CancellationToken cancellationToken) =>
        RunInteractionAsync(id, returnUrl, () => resourceService.ToggleHelpfulAsync(id, RequireUserId(), cancellationToken), "Marked as helpful. Thank you.", "Helpful vote removed.");

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rate(long id, int rating, CancellationToken cancellationToken)
    {
        try
        {
            await resourceService.RateAsync(id, RequireUserId(), rating, cancellationToken);
            TempData["SuccessMessage"] = "Your rating was saved.";
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (DomainValidationException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<IActionResult> RunInteractionAsync(
        long id,
        string? returnUrl,
        Func<Task<ResourceInteractionResult>> action,
        string activatedMessage,
        string deactivatedMessage)
    {
        try
        {
            var result = await action();
            TempData["SuccessMessage"] = result.IsActive ? activatedMessage : deactivatedMessage;
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (DomainValidationException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }

        return SafeReturn(returnUrl, nameof(Details), new { id });
    }

    internal static ResourceType? ParseResourceType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Replace(" ", "", StringComparison.Ordinal).Replace("-", "", StringComparison.Ordinal);
        return Enum.TryParse<ResourceType>(normalized, true, out var parsed) && Enum.IsDefined(parsed) ? parsed : null;
    }

    private static ResourceSort ParseResourceSort(string? sort) => sort?.Trim().ToLowerInvariant() switch
    {
        "mosthelpful" or "helpful" => ResourceSort.MostHelpful,
        "newest" or "recent" => ResourceSort.Newest,
        "title" => ResourceSort.Title,
        _ => ResourceSort.Quality
    };

    private static IReadOnlyList<ResourceTypeFilterViewModel> BuildResourceTypes(ResourceType? selected)
    {
        var options = new List<ResourceTypeFilterViewModel>
        {
            new() { Value = "", Label = "All resources", Icon = "grid", IsSelected = selected is null }
        };
        options.AddRange(Enum.GetValues<ResourceType>().Select(value => new ResourceTypeFilterViewModel
        {
            Value = value.ToString(),
            Label = FeatureMappings.DisplayEnum(value) switch
            {
                "Previous Exam" => "Previous exams",
                "Quiz Bank" => "Quiz banks",
                "Lecture Summary" => "Lecture summaries",
                "Cheat Sheet" => "Cheat sheets",
                "Student Note" => "Student notes",
                "Recommended Video" => "Videos",
                "External Resource" => "External links",
                var label => label + "s"
            },
            Icon = ResourceTypeIcon(value),
            IsSelected = value == selected
        }));
        return options;
    }

    private static ExplorerResourceCardViewModel ToExplorerCard(ResourceListItem item) => new()
    {
        Id = item.Id,
        CourseId = item.CourseId,
        CourseCode = item.CourseCode,
        CourseName = item.CourseName,
        Title = item.Title,
        Type = FeatureMappings.DisplayEnum(item.Type),
        TypeKey = Slug(item.Type.ToString()),
        Semester = item.Semester,
        UploaderName = item.UploaderName,
        AverageRating = item.AverageRating,
        RatingCount = item.RatingCount,
        HelpfulCount = item.HelpfulCount,
        IsVerified = item.IsVerified,
        IsRepresentative = item.IsRepresentative,
        IsSaved = item.IsSaved,
        Tags = item.Tags,
        PublishedAtUtc = item.PublishedAtUtc
    };

    private static string ResourceTypeIcon(ResourceType type) => type switch
    {
        ResourceType.PreviousExam => "exam",
        ResourceType.Assignment => "clipboard",
        ResourceType.QuizBank => "quiz",
        ResourceType.LectureSummary => "notes",
        ResourceType.CheatSheet => "bolt",
        ResourceType.StudentNote => "file",
        ResourceType.RecommendedVideo => "play",
        ResourceType.ExternalResource => "link",
        _ => "file"
    };

    private static string Slug(string value) => string.Concat(value.Select((character, index) =>
        index > 0 && char.IsUpper(character) ? "-" + char.ToLowerInvariant(character) : char.ToLowerInvariant(character).ToString()));

    private IActionResult SafeReturn(string? returnUrl, string fallbackAction, object? routeValues = null) =>
        !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction(fallbackAction, routeValues);

    private string RequireUserId() => userManager.GetUserId(User)
        ?? throw new UnauthorizedAccessException("An authenticated user is required.");

    private sealed record AcademicCourseRow(
        int Id,
        string Code,
        string Name,
        string Instructor,
        int MajorId,
        string MajorName,
        int UniversityId,
        string UniversityName);

    private sealed record UserCourseRow(int CourseId, bool IsActive);
}
