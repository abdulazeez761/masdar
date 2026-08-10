using Masdar.Web.Data;
using Masdar.Web.Domain.Entities;
using Masdar.Web.Domain.Enums;
using Masdar.Web.Services;
using Masdar.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Masdar.Web.Controllers;

public sealed class CoursesController(
    ICourseService courseService,
    IResourceService resourceService,
    IOpportunityService opportunityService,
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        int? universityId,
        int? majorId,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        var result = await courseService.SearchAsync(
            new CourseQuery(search, universityId, majorId, page, 12), cancellationToken);

        int? activeCourseId = null;
        var userId = userManager.GetUserId(User);
        if (userId is not null)
        {
            activeCourseId = (await courseService.GetActiveCourseAsync(userId, cancellationToken))?.Id;
        }

        var universities = await dbContext.Universities.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem(x.Name, x.Id.ToString(), x.Id == universityId))
            .ToListAsync(cancellationToken);
        var majors = await dbContext.Majors.AsNoTracking()
            .Where(x => x.IsActive && (!universityId.HasValue || x.UniversityId == universityId))
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem(x.Name, x.Id.ToString(), x.Id == majorId))
            .ToListAsync(cancellationToken);

        return View(new CourseCatalogViewModel
        {
            Search = search,
            UniversityId = universityId,
            MajorId = majorId,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount,
            Universities = universities,
            Majors = majors,
            Courses = result.Items.Select(x => x.ToViewModel(x.Id == activeCourseId)).ToArray()
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, string tab = "overview", CancellationToken cancellationToken = default)
    {
        var validTabs = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "overview", "resources", "roadmap", "career" };
        tab = validTabs.Contains(tab) ? tab.ToLowerInvariant() : "overview";
        var userId = userManager.GetUserId(User);
        var workspace = await courseService.GetWorkspaceAsync(id, userId, cancellationToken);
        if (workspace is null)
        {
            return NotFound();
        }

        var opportunities = await opportunityService.ListAsync(userId, cancellationToken);
        var courseResources = tab == "resources"
            ? (await resourceService.SearchAsync(new ResourceQuery(
                CourseIds: [id],
                Sort: ResourceSort.Quality,
                PageSize: 30), userId, cancellationToken)).Items
            : workspace.TopResources;
        int? activeCourseId = null;
        if (userId is not null)
        {
            activeCourseId = (await courseService.GetActiveCourseAsync(userId, cancellationToken))?.Id;
        }

        return View(new CourseDetailsViewModel
        {
            Course = workspace.Course.ToViewModel(workspace.Course.Id == activeCourseId, workspace.ReadinessPercent),
            Tab = tab,
            Resources = courseResources.Select(x => x.ToViewModel()).ToArray(),
            Roadmap = workspace.Roadmap.Select(x => x.ToViewModel()).ToArray(),
            Opportunities = opportunities.Take(6).Select(x => x.ToViewModel()).ToArray(),
            CareerConnections = BuildCareerConnections(workspace.Course)
        });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetActive(
        int id,
        string? returnUrl,
        CancellationToken cancellationToken)
    {
        try
        {
            await courseService.SetActiveCourseAsync(id, RequireUserId(), cancellationToken);
            TempData["SuccessMessage"] = "Your active course was updated.";
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction(nameof(Details), new { id });
        }
        catch (DomainValidationException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
            return RedirectToAction(nameof(Index));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleRoadmap(int courseId, int roadmapNodeId, CancellationToken cancellationToken)
    {
        try
        {
            var isCompleted = await courseService.ToggleRoadmapProgressAsync(roadmapNodeId, RequireUserId(), cancellationToken);
            TempData["SuccessMessage"] = isCompleted ? "Topic marked complete." : "Topic marked incomplete.";
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (DomainValidationException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }

        return RedirectToAction(nameof(Details), new { id = courseId, tab = "roadmap" });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleOpportunity(long id, string? returnUrl, CancellationToken cancellationToken)
    {
        try
        {
            var result = await opportunityService.ToggleRegistrationAsync(id, RequireUserId(), cancellationToken);
            TempData["SuccessMessage"] = result.IsRegistered ? "Your RSVP is confirmed." : "Your RSVP was cancelled.";
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (DomainValidationException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }

        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction(nameof(Index));
    }

    private string RequireUserId() => userManager.GetUserId(User)
        ?? throw new UnauthorizedAccessException("An authenticated user is required.");

    private static IReadOnlyList<CareerConnectionViewModel> BuildCareerConnections(CourseListItem course) =>
    [
        new()
        {
            Title = "Technical foundations",
            Description = $"{course.Name} builds concepts used when designing, analyzing, and improving real technical systems."
        },
        new()
        {
            Title = "Structured problem solving",
            Description = "The course workflow—model, test, explain, and revise—transfers directly to engineering and analytical roles."
        },
        new()
        {
            Title = "Portfolio practice",
            Description = $"Turn one representative {course.Code} topic into a small explained project or case study to demonstrate applied understanding."
        }
    ];
}
