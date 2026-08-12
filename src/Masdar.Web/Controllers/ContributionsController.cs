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

[Authorize]
public sealed class ContributionsController(
    IResourceService resourceService,
    ICommunityService communityService,
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Create(int? courseId, string? type, long? requestId, CancellationToken cancellationToken)
    {
        var model = new ContributionCreateViewModel
        {
            CourseId = courseId,
            Type = ResourcesController.ParseResourceType(type)?.ToString(),
            RequestId = requestId
        };

        if (requestId.HasValue)
        {
            var request = await dbContext.ResourceRequests.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == requestId && x.Status == ResourceRequestStatus.Open, cancellationToken);
            if (request is null)
            {
                return NotFound();
            }

            model.CourseId = request.CourseId;
            model.Type = request.DesiredResourceType.ToString();
            var title = $"Fulfillment: {request.Title}";
            model.Title = title.Length > 220 ? title[..220] : title;
            var desc = request.Description?.Trim();
            model.Description = (!string.IsNullOrWhiteSpace(desc) && desc.Length >= 10)
                ? desc
                : $"Material contributed for community request #{request.Id}.";
        }

        await PopulateOptionsAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024 + 256 * 1024)]
    public async Task<IActionResult> Create(ContributionCreateViewModel model, CancellationToken cancellationToken)
    {
        var type = ResourcesController.ParseResourceType(model.Type);
        if (type is null)
        {
            ModelState.AddModelError(nameof(model.Type), "Choose a valid resource type.");
        }

        if (!ModelState.IsValid || model.CourseId is null || model.File is null || type is null)
        {
            await PopulateOptionsAsync(model, cancellationToken);
            return View(model);
        }

        try
        {
            var tags = (model.Tags ?? "")
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(8)
                .ToArray();
            var resourceId = await resourceService.CreateAsync(new CreateResourceCommand(
                model.CourseId.Value,
                model.Title,
                type.Value,
                model.Semester,
                model.Description,
                model.TutorContext,
                tags,
                model.File,
                model.HasSharingConsent), RequireUserId(), cancellationToken);

            if (model.RequestId.HasValue)
            {
                try
                {
                    await communityService.FulfillRequestAsync(model.RequestId.Value, resourceId, RequireUserId(), cancellationToken);
                    TempData["SuccessMessage"] = "Resource published and community request fulfilled.";
                }
                catch (DomainValidationException exception)
                {
                    TempData["WarningMessage"] = $"Your resource was published, but the request could not be fulfilled: {exception.Message}";
                }
            }
            else
            {
                TempData["SuccessMessage"] = "Your resource is now published for the community.";
            }

            return RedirectToAction("Details", "Resources", new { id = resourceId });
        }
        catch (DomainValidationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
        }
        catch (InvalidDataException exception)
        {
            ModelState.AddModelError(nameof(model.File), exception.Message);
        }

        await PopulateOptionsAsync(model, cancellationToken);
        return View(model);
    }

    private async Task PopulateOptionsAsync(ContributionCreateViewModel model, CancellationToken cancellationToken)
    {
        var courses = await dbContext.Courses.AsNoTracking()
            .Where(x => x.IsActive && x.Major.IsActive && x.Major.University.IsActive)
            .OrderBy(x => x.Code)
            .Select(x => new { x.Id, Label = x.Code + " · " + x.Name })
            .ToListAsync(cancellationToken);
        model.Courses = courses.Select(x => new SelectListItem(x.Label, x.Id.ToString(), x.Id == model.CourseId)).ToArray();
        model.Types = Enum.GetValues<ResourceType>().Select(x => new SelectListItem(FeatureMappings.DisplayEnum(x), x.ToString(), x.ToString() == model.Type)).ToArray();
    }

    private string RequireUserId() => userManager.GetUserId(User)
        ?? throw new UnauthorizedAccessException("An authenticated user is required.");
}
