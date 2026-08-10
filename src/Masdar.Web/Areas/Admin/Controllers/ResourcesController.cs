using Masdar.Web.Areas.Admin.ViewModels;
using Masdar.Web.Data;
using Masdar.Web.Domain.Entities;
using Masdar.Web.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Masdar.Web.Areas.Admin.Controllers;

public sealed class ResourcesController(ApplicationDbContext db) : AdminControllerBase(db)
{
    public async Task<IActionResult> Index(
        string? query,
        ResourcePublicationStatus? status,
        bool attention = false,
        CancellationToken cancellationToken = default)
    {
        var items = Db.StudyResources.AsNoTracking().Include(x => x.Course).Include(x => x.Uploader).AsQueryable();
        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            items = items.Where(x => x.Title.Contains(term) || x.Course.Code.Contains(term) || x.Uploader.DisplayName.Contains(term));
        }
        if (status.HasValue) items = items.Where(x => x.PublicationStatus == status);
        if (attention)
        {
            items = items.Where(resource =>
                resource.PublicationStatus == ResourcePublicationStatus.PendingReview ||
                resource.FileScanStatus == FileScanStatus.Pending ||
                resource.FileScanStatus == FileScanStatus.Failed ||
                resource.FileScanStatus == FileScanStatus.Infected);
        }

        ViewBag.Query = query;
        ViewBag.Status = status;
        ViewBag.Attention = attention;
        return View(await items.OrderByDescending(x => x.CreatedAtUtc).Take(300).ToListAsync(cancellationToken));
    }

    public async Task<IActionResult> Details(long id, CancellationToken cancellationToken)
    {
        var entity = await Db.StudyResources.AsNoTracking().Include(x => x.Course).ThenInclude(x => x.Major).ThenInclude(x => x.University)
            .Include(x => x.Uploader).Include(x => x.ResourceTags).ThenInclude(x => x.Tag)
            .Include(x => x.Ratings).Include(x => x.HelpfulVotes).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();
        ViewBag.Moderation = new ResourceModerationInputModel
        {
            Id = entity.Id, PublicationStatus = entity.PublicationStatus,
            VerificationStatus = entity.VerificationStatus, IsRepresentative = entity.IsRepresentative
        };
        return View(entity);
    }

    [HttpPost]
    public async Task<IActionResult> Moderate(long id, ResourceModerationInputModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id) return BadRequest();
        var entity = await Db.StudyResources.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Choose valid moderation settings.";
            return RedirectToAction(nameof(Details), new { id });
        }

        entity.PublicationStatus = model.PublicationStatus;
        entity.VerificationStatus = model.VerificationStatus;
        entity.IsRepresentative = model.IsRepresentative;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        entity.PublishedAtUtc = model.PublicationStatus == ResourcePublicationStatus.Published ? entity.PublishedAtUtc ?? DateTime.UtcNow : entity.PublishedAtUtc;
        entity.ArchivedAtUtc = model.PublicationStatus == ResourcePublicationStatus.Archived ? DateTime.UtcNow : null;
        AddAudit("ResourceModerated", nameof(StudyResource), id.ToString(), $"Moderated '{entity.Title}'. {model.Note}".Trim());
        await Db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Moderation decision saved.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Archive(long id, CancellationToken cancellationToken)
    {
        var entity = await Db.StudyResources.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();
        entity.PublicationStatus = ResourcePublicationStatus.Archived; entity.ArchivedAtUtc = DateTime.UtcNow; entity.UpdatedAtUtc = DateTime.UtcNow;
        AddAudit("ResourceArchived", nameof(StudyResource), id.ToString(), $"Archived '{entity.Title}'.");
        await Db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Resource archived.";
        return RedirectToAction(nameof(Index));
    }
}
