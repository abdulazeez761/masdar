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

public sealed class CommunityController(
    ICommunityService communityService,
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        string tab = "requests",
        string? search = null,
        int? courseId = null,
        string? type = null,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        tab = string.Equals(tab, "contributors", StringComparison.OrdinalIgnoreCase) ? "contributors" : "requests";
        var resourceType = ResourcesController.ParseResourceType(type);
        PagedResult<CommunityRequestItem> requests = new([], 1, 10, 0);
        IReadOnlyList<LeaderboardItem> leaders = [];
        if (tab == "contributors")
        {
            leaders = await communityService.GetLeaderboardAsync(25, cancellationToken);
        }
        else
        {
            try
            {
                requests = await communityService.SearchRequestsAsync(new CommunityRequestQuery(
                    search,
                    courseId,
                    resourceType,
                    ResourceRequestStatus.Open,
                    Math.Max(1, page),
                    10), userManager.GetUserId(User), cancellationToken);
            }
            catch (DomainValidationException exception)
            {
                TempData["ErrorMessage"] = exception.Message;
            }
        }

        var courseRows = await ActiveCoursesAsync(cancellationToken);
        var currentUserId = userManager.GetUserId(User);
        return View(new CommunityIndexViewModel
        {
            Tab = tab,
            Search = search,
            CourseId = courseId,
            Type = type,
            Page = requests.Page,
            PageSize = requests.PageSize,
            TotalCount = requests.TotalCount,
            Courses = courseRows.Select(x => new SelectListItem(x.Label, x.Id.ToString(), x.Id == courseId)).ToArray(),
            Types = Enum.GetValues<ResourceType>().Select(x => new SelectListItem(FeatureMappings.DisplayEnum(x), x.ToString(), x == resourceType)).ToArray(),
            Requests = requests.Items.Select(x => new ResourceRequestViewModel
            {
                Id = x.Id,
                CourseId = x.CourseId,
                CourseCode = x.CourseCode,
                Type = FeatureMappings.DisplayEnum(x.DesiredResourceType),
                Title = x.Title,
                Notes = x.Description,
                RequesterName = x.RequesterName,
                VoteCount = x.VoteCount,
                BountyPoints = x.BountyPoints,
                Status = FeatureMappings.DisplayEnum(x.Status),
                HasVoted = x.HasCurrentUserVoted,
                CreatedAtUtc = x.CreatedAtUtc
            }).ToArray(),
            Leaders = leaders.Select((x, index) => new LeaderboardEntryViewModel
            {
                Rank = index + 1,
                UserId = x.UserId,
                DisplayName = x.DisplayName,
                Department = x.MajorName,
                ResourceCount = x.UploadCount,
                Points = x.ReputationBalance,
                IsCurrentUser = x.UserId == currentUserId
            }).ToArray()
        });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Create(int? courseId, CancellationToken cancellationToken)
    {
        var model = new CreateResourceRequestViewModel { CourseId = courseId };
        await PopulateOptionsAsync(model, cancellationToken);
        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateResourceRequestViewModel model, CancellationToken cancellationToken)
    {
        var type = ResourcesController.ParseResourceType(model.Type);
        if (type is null)
        {
            ModelState.AddModelError(nameof(model.Type), "Choose a valid resource type.");
        }

        if (!ModelState.IsValid || model.CourseId is null || type is null)
        {
            await PopulateOptionsAsync(model, cancellationToken);
            return View(model);
        }

        try
        {
            await communityService.CreateRequestAsync(new CreateResourceRequestCommand(
                model.CourseId.Value,
                type.Value,
                model.Title,
                model.Notes,
                model.BountyPoints), RequireUserId(), cancellationToken);
            TempData["SuccessMessage"] = "Your request is now visible to the community.";
            return RedirectToAction(nameof(Index));
        }
        catch (DomainValidationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            await PopulateOptionsAsync(model, cancellationToken);
            return View(model);
        }
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleVote(long id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await communityService.ToggleRequestVoteAsync(id, RequireUserId(), cancellationToken);
            TempData["SuccessMessage"] = result.IsActive ? "Request upvoted." : "Your vote was removed.";
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (DomainValidationException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateOptionsAsync(CreateResourceRequestViewModel model, CancellationToken cancellationToken)
    {
        var courseRows = await ActiveCoursesAsync(cancellationToken);
        model.Courses = courseRows.Select(x => new SelectListItem(x.Label, x.Id.ToString(), x.Id == model.CourseId)).ToArray();
        model.Types = Enum.GetValues<ResourceType>().Select(x => new SelectListItem(FeatureMappings.DisplayEnum(x), x.ToString(), x.ToString() == model.Type)).ToArray();
    }

    private Task<List<CourseOption>> ActiveCoursesAsync(CancellationToken cancellationToken) => dbContext.Courses.AsNoTracking()
        .Where(x => x.IsActive && x.Major.IsActive && x.Major.University.IsActive)
        .OrderBy(x => x.Code)
        .Select(x => new CourseOption(x.Id, x.Code + " · " + x.Name))
        .ToListAsync(cancellationToken);

    private string RequireUserId() => userManager.GetUserId(User)
        ?? throw new UnauthorizedAccessException("An authenticated user is required.");

    private sealed record CourseOption(int Id, string Label);
}
