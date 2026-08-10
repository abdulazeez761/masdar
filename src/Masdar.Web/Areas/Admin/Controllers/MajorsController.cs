using Masdar.Web.Areas.Admin.ViewModels;
using Masdar.Web.Data;
using Masdar.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Masdar.Web.Areas.Admin.Controllers;

public sealed class MajorsController(ApplicationDbContext db) : AdminControllerBase(db)
{
    public async Task<IActionResult> Index(int? universityId, CancellationToken cancellationToken)
    {
        var query = Db.Majors.AsNoTracking().Include(x => x.University).Include(x => x.Courses).AsQueryable();
        if (universityId.HasValue) query = query.Where(x => x.UniversityId == universityId);
        ViewBag.UniversityId = universityId;
        ViewBag.Universities = await Db.Universities.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);
        return View(await query.OrderBy(x => x.University.Name).ThenBy(x => x.Name).ToListAsync(cancellationToken));
    }

    public async Task<IActionResult> Create(int? universityId, CancellationToken cancellationToken)
    {
        await LoadUniversities(cancellationToken);
        return View("Edit", new MajorInputModel { UniversityId = universityId ?? 0 });
    }

    [HttpPost]
    public async Task<IActionResult> Create(MajorInputModel model, CancellationToken cancellationToken)
    {
        await Validate(model, null, cancellationToken);
        if (!ModelState.IsValid) { await LoadUniversities(cancellationToken); return View("Edit", model); }
        var entity = new Major { UniversityId = model.UniversityId, Name = model.Name.Trim(), NormalizedName = Normalize(model.Name), IsActive = model.IsActive };
        Db.Majors.Add(entity); AddAudit("MajorCreated", nameof(Major), null, $"Created major {entity.Name}.");
        await Db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Major created.";
        return RedirectToAction(nameof(Index), new { universityId = model.UniversityId });
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var entity = await Db.Majors.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();
        await LoadUniversities(cancellationToken);
        return View(new MajorInputModel { Id = entity.Id, UniversityId = entity.UniversityId, Name = entity.Name, IsActive = entity.IsActive });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, MajorInputModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id) return BadRequest();
        var entity = await Db.Majors.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();
        await Validate(model, id, cancellationToken);
        if (!ModelState.IsValid) { await LoadUniversities(cancellationToken); return View(model); }
        entity.UniversityId = model.UniversityId; entity.Name = model.Name.Trim(); entity.NormalizedName = Normalize(model.Name);
        entity.IsActive = model.IsActive; entity.UpdatedAtUtc = DateTime.UtcNow;
        AddAudit("MajorUpdated", nameof(Major), id.ToString(), $"Updated major {entity.Name}.");
        await Db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Major updated.";
        return RedirectToAction(nameof(Index), new { universityId = entity.UniversityId });
    }

    [HttpPost]
    public async Task<IActionResult> Toggle(int id, CancellationToken cancellationToken)
    {
        var entity = await Db.Majors.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();
        entity.IsActive = !entity.IsActive; entity.UpdatedAtUtc = DateTime.UtcNow;
        AddAudit(entity.IsActive ? "MajorActivated" : "MajorDeactivated", nameof(Major), id.ToString(), $"Changed status for {entity.Name}.");
        await Db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Major status updated.";
        return RedirectToAction(nameof(Index), new { universityId = entity.UniversityId });
    }

    private async Task Validate(MajorInputModel model, int? excludedId, CancellationToken cancellationToken)
    {
        if (!await Db.Universities.AnyAsync(x => x.Id == model.UniversityId, cancellationToken)) ModelState.AddModelError(nameof(model.UniversityId), "Select a valid university.");
        var normalized = Normalize(model.Name);
        if (await Db.Majors.AnyAsync(x => x.UniversityId == model.UniversityId && x.NormalizedName == normalized && (!excludedId.HasValue || x.Id != excludedId), cancellationToken))
            ModelState.AddModelError(nameof(model.Name), "This major already exists at the selected university.");
    }

    private async Task LoadUniversities(CancellationToken cancellationToken) => ViewBag.Universities = await Db.Universities.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);
    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
}
