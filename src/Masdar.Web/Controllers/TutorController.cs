using Masdar.Web.Domain.Entities;
using Masdar.Web.Services;
using Masdar.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Masdar.Web.Controllers;

[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class TutorController(
    ITutorService tutorService,
    UserManager<ApplicationUser> userManager) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? question, CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        question = question?.Trim();
        if (!string.IsNullOrWhiteSpace(question) && question.Length is >= 2 and <= 800)
        {
            var result = await tutorService.AskAsync(
                userId,
                question,
                expectedCourseId: null,
                cancellationToken);
            return View(new TutorPageViewModel
            {
                Workspace = result.Workspace,
                Exchange = result.Exchange,
                ContextCourseId = result.Workspace.ActiveCourse?.Id
            });
        }

        var workspace = await tutorService.GetWorkspaceAsync(userId, cancellationToken);
        var model = new TutorPageViewModel
        {
            Workspace = workspace,
            ContextCourseId = workspace.ActiveCourse?.Id,
            Question = question is null ? string.Empty : question[..Math.Min(question.Length, 800)]
        };
        if (question is not null)
        {
            ModelState.AddModelError(nameof(model.Question), "Your question must be between 2 and 800 characters.");
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(
        [Bind("Question,ContextCourseId")] TutorPageViewModel model,
        CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        if (!ModelState.IsValid)
        {
            model.Workspace = await tutorService.GetWorkspaceAsync(userId, cancellationToken);
            model.ContextCourseId = model.Workspace.ActiveCourse?.Id;
            return View(model);
        }

        try
        {
            var result = await tutorService.AskAsync(
                userId,
                model.Question,
                model.ContextCourseId,
                cancellationToken);
            model.Workspace = result.Workspace;
            model.Exchange = result.Exchange;
            model.ContextChanged = result.ContextChanged;
            model.ContextCourseId = result.Workspace.ActiveCourse?.Id;
            model.Question = string.Empty;

            // Successful posts start a fresh composer. Clearing ModelState is required
            // because input tag helpers otherwise prefer the stale posted course guard
            // and question over the server-resolved active course values above.
            ModelState.Clear();
        }
        catch (DomainValidationException exception)
        {
            ModelState.AddModelError(nameof(model.Question), exception.Message);
            model.Workspace = await tutorService.GetWorkspaceAsync(userId, cancellationToken);
            model.ContextCourseId = model.Workspace.ActiveCourse?.Id;
        }

        return View(model);
    }

    private string RequireUserId() => userManager.GetUserId(User)
        ?? throw new UnauthorizedAccessException("An authenticated user is required.");
}
