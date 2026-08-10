using Masdar.Web.Data;
using Masdar.Web.Domain.Entities;
using Masdar.Web.Domain.Enums;
using Masdar.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Masdar.Web.Controllers;

[Authorize]
public sealed class ProfileController(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        var user = await dbContext.Users.AsNoTracking()
            .Include(x => x.University)
            .Include(x => x.Major)
            .SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null)
        {
            return Challenge();
        }

        var uploadedCount = await dbContext.StudyResources.CountAsync(x => x.UploaderId == userId, cancellationToken);
        var savedCount = await dbContext.SavedResources.CountAsync(x => x.UserId == userId, cancellationToken);
        var completedCount = await dbContext.UserRoadmapProgress.CountAsync(x => x.UserId == userId && x.IsCompleted, cancellationToken);
        var recent = await dbContext.StudyResources.AsNoTracking()
            .Where(x => x.UploaderId == userId && x.PublicationStatus == ResourcePublicationStatus.Published)
            .Include(x => x.Course)
            .Include(x => x.Uploader)
            .Include(x => x.Ratings)
            .Include(x => x.HelpfulVotes)
            .Include(x => x.SavedByUsers)
            .Include(x => x.ResourceTags).ThenInclude(x => x.Tag)
            .OrderByDescending(x => x.PublishedAtUtc)
            .Take(4)
            .ToListAsync(cancellationToken);

        return View(new ProfileViewModel
        {
            DisplayName = user.DisplayName,
            Email = user.Email ?? "",
            Bio = user.Bio,
            University = user.University?.Name,
            Major = user.Major?.Name,
            Department = user.Major?.Name,
            ReputationPoints = user.ReputationBalance,
            UploadedResourceCount = uploadedCount,
            SavedResourceCount = savedCount,
            CompletedRoadmapCount = completedCount,
            RecentUploads = recent.Select(x => new ResourceCardViewModel
            {
                Id = x.Id,
                CourseId = x.CourseId,
                CourseCode = x.Course.Code,
                Title = x.Title,
                Type = FeatureMappings.DisplayEnum(x.Type),
                Semester = x.Semester,
                Description = x.Description,
                UploaderName = user.DisplayName,
                AverageRating = x.Ratings.Count == 0 ? 0 : x.Ratings.Average(rating => rating.Value),
                RatingCount = x.Ratings.Count,
                HelpfulCount = x.HelpfulVotes.Count,
                IsVerified = x.VerificationStatus == ResourceVerificationStatus.Verified,
                IsRepresentative = x.IsRepresentative,
                IsSaved = x.SavedByUsers.Any(saved => saved.UserId == userId),
                Tags = x.ResourceTags.Select(tag => tag.Tag.Name).OrderBy(tag => tag).ToArray()
            }).ToArray()
        });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var model = new EditProfileViewModel
        {
            DisplayName = user.DisplayName,
            Bio = user.Bio,
            UniversityId = user.UniversityId,
            MajorId = user.MajorId
        };
        await PopulateOptionsAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditProfileViewModel model, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        if (model.MajorId.HasValue)
        {
            var major = await dbContext.Majors.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == model.MajorId && x.IsActive && x.University.IsActive, cancellationToken);
            if (major is null || (model.UniversityId.HasValue && major.UniversityId != model.UniversityId))
            {
                ModelState.AddModelError(nameof(model.MajorId), "Choose a major from the selected university.");
            }
            else
            {
                model.UniversityId = major.UniversityId;
            }
        }
        else if (model.UniversityId.HasValue && !await dbContext.Universities.AnyAsync(x => x.Id == model.UniversityId && x.IsActive, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.UniversityId), "Choose an active university.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model, cancellationToken);
            return View(model);
        }

        user.DisplayName = model.DisplayName.Trim();
        user.Bio = string.IsNullOrWhiteSpace(model.Bio) ? null : model.Bio.Trim();
        user.UniversityId = model.UniversityId;
        user.MajorId = model.MajorId;
        user.UpdatedAtUtc = DateTime.UtcNow;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            await PopulateOptionsAsync(model, cancellationToken);
            return View(model);
        }

        TempData["SuccessMessage"] = "Your profile was updated.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateOptionsAsync(EditProfileViewModel model, CancellationToken cancellationToken)
    {
        var universities = await dbContext.Universities.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync(cancellationToken);
        var majors = await dbContext.Majors.AsNoTracking().Where(x => x.IsActive && x.University.IsActive).Include(x => x.University).OrderBy(x => x.University.Name).ThenBy(x => x.Name).ToListAsync(cancellationToken);
        model.Universities = universities.Select(x => new SelectListItem(x.Name, x.Id.ToString(), x.Id == model.UniversityId)).ToArray();
        model.Majors = majors.Select(x => new SelectListItem($"{x.University.Name} · {x.Name}", x.Id.ToString(), x.Id == model.MajorId)).ToArray();
    }

    private string RequireUserId() => userManager.GetUserId(User)
        ?? throw new UnauthorizedAccessException("An authenticated user is required.");
}
