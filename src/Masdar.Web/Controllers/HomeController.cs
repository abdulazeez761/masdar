using System.Diagnostics;
using Masdar.Web.Domain.Entities;
using Masdar.Web.Models;
using Masdar.Web.Services;
using Masdar.Web.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Masdar.Web.Controllers;

public sealed class HomeController(
    ICourseService courseService,
    UserManager<ApplicationUser> userManager,
    ILogger<HomeController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User);
        if (userId is null)
        {
            return View(new HomeDashboardViewModel());
        }

        var dashboard = await courseService.GetDashboardAsync(userId, cancellationToken);
        CourseWorkspace? workspace = null;
        if (dashboard.ActiveCourse is not null)
        {
            workspace = await courseService.GetWorkspaceAsync(dashboard.ActiveCourse.Id, userId, cancellationToken);
        }

        var user = await userManager.GetUserAsync(User);
        return View(new HomeDashboardViewModel
        {
            IsAuthenticated = true,
            DisplayName = user?.DisplayName ?? User.Identity?.Name ?? "Student",
            ActiveCourse = dashboard.ActiveCourse?.ToViewModel(true, workspace?.ReadinessPercent ?? 0),
            Courses = dashboard.ActiveCourse is null ? [] : [dashboard.ActiveCourse.ToViewModel(true, workspace?.ReadinessPercent ?? 0)],
            RecommendedResources = dashboard.RecommendedResources.Select(x => x.ToViewModel()).ToArray(),
            Roadmap = workspace?.Roadmap.Select(x => x.ToViewModel()).ToArray() ?? [],
            Opportunities = dashboard.UpcomingOpportunities.Select(x => x.ToViewModel()).ToArray(),
            CompletedRoadmapNodes = dashboard.CompletedRoadmapNodeCount,
            SavedResources = dashboard.SavedResourceCount,
            ReputationPoints = dashboard.ReputationBalance
        });
    }

    [HttpGet]
    public IActionResult Privacy() => View();

    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        logger.LogWarning("Rendering the error page for request {RequestId}.", HttpContext.TraceIdentifier);
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
