using Masdar.Web.Areas.Admin.ViewModels;
using Masdar.Web.Data;
using Masdar.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Masdar.Web.Areas.Admin.Controllers;

public sealed class UniversitiesController(ApplicationDbContext db) : AdminControllerBase(db)
{
    public async Task<IActionResult> Index(string? query, CancellationToken cancellationToken)
    {
        var items = Db.Universities.AsNoTracking().Include(x => x.Majors).AsQueryable();
        if (!string.IsNullOrWhiteSpace(query)) items = items.Where(x => x.Name.Contains(query.Trim()));
        ViewBag.Query = query;
        return View(await items.OrderBy(x => x.Name).ToListAsync(cancellationToken));
    }

    public IActionResult Create() => View("Edit", new UniversityInputModel());

    [HttpPost]
    public async Task<IActionResult> Create(UniversityInputModel model, CancellationToken cancellationToken)
    {
        var normalized = Normalize(model.Name);
        if (await Db.Universities.AnyAsync(x => x.NormalizedName == normalized, cancellationToken))
            ModelState.AddModelError(nameof(model.Name), "A university with this name already exists.");
        if (!ModelState.IsValid) return View("Edit", model);

        var entity = new University { Name = model.Name.Trim(), NormalizedName = normalized, Location = Clean(model.Location), WebsiteUrl = Clean(model.WebsiteUrl), IsActive = model.IsActive };
        Db.Universities.Add(entity);
        AddAudit("UniversityCreated", nameof(University), null, $"Created university {entity.Name}.");
        await Db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "University created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var entity = await Db.Universities.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();
        return View(new UniversityInputModel { Id = entity.Id, Name = entity.Name, Location = entity.Location, WebsiteUrl = entity.WebsiteUrl, IsActive = entity.IsActive });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, UniversityInputModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id) return BadRequest();
        var entity = await Db.Universities.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();
        var normalized = Normalize(model.Name);
        if (await Db.Universities.AnyAsync(x => x.Id != id && x.NormalizedName == normalized, cancellationToken))
            ModelState.AddModelError(nameof(model.Name), "A university with this name already exists.");
        if (!ModelState.IsValid) return View(model);

        entity.Name = model.Name.Trim(); entity.NormalizedName = normalized; entity.Location = Clean(model.Location);
        entity.WebsiteUrl = Clean(model.WebsiteUrl); entity.IsActive = model.IsActive; entity.UpdatedAtUtc = DateTime.UtcNow;
        AddAudit("UniversityUpdated", nameof(University), id.ToString(), $"Updated university {entity.Name}.");
        await Db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "University updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Toggle(int id, CancellationToken cancellationToken)
    {
        var entity = await Db.Universities.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();
        entity.IsActive = !entity.IsActive; entity.UpdatedAtUtc = DateTime.UtcNow;
        AddAudit(entity.IsActive ? "UniversityActivated" : "UniversityDeactivated", nameof(University), id.ToString(), $"Changed status for {entity.Name}.");
        await Db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "University status updated.";
        return RedirectToAction(nameof(Index));
    }

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
