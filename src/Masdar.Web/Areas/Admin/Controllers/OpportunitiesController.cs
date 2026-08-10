using Masdar.Web.Areas.Admin.ViewModels;
using Masdar.Web.Data;
using Masdar.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Masdar.Web.Areas.Admin.Controllers;

public sealed class OpportunitiesController(ApplicationDbContext db) : AdminControllerBase(db)
{
    public async Task<IActionResult> Index(string? query, CancellationToken cancellationToken)
    {
        var items = Db.Opportunities.AsNoTracking().Include(x => x.Registrations).AsQueryable();
        if (!string.IsNullOrWhiteSpace(query)) items = items.Where(x => x.Title.Contains(query.Trim()));
        ViewBag.Query = query;
        return View(await items.OrderByDescending(x => x.StartsAtUtc).Take(300).ToListAsync(cancellationToken));
    }

    public IActionResult Create() => View("Edit", new OpportunityInputModel());

    [HttpPost]
    public async Task<IActionResult> Create(OpportunityInputModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View("Edit", model);
        var entity = new Opportunity(); Map(model, entity);
        Db.Opportunities.Add(entity); AddAudit("OpportunityCreated", nameof(Opportunity), null, $"Created opportunity {entity.Title}.");
        await Db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Opportunity created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(long id, CancellationToken cancellationToken)
    {
        var x = await Db.Opportunities.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (x is null) return NotFound();
        return View(new OpportunityInputModel
        {
            Id = x.Id, Type = x.Type, Title = x.Title, Description = x.Description, Location = x.Location,
            RegistrationUrl = x.RegistrationUrl, StartsAtUtc = x.StartsAtUtc, EndsAtUtc = x.EndsAtUtc,
            RegistrationDeadlineUtc = x.RegistrationDeadlineUtc, Capacity = x.Capacity, IsPublished = x.IsPublished, IsActive = x.IsActive
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(long id, OpportunityInputModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id) return BadRequest();
        var entity = await Db.Opportunities.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();
        if (!ModelState.IsValid) return View(model);
        Map(model, entity); entity.UpdatedAtUtc = DateTime.UtcNow;
        AddAudit("OpportunityUpdated", nameof(Opportunity), id.ToString(), $"Updated opportunity {entity.Title}.");
        await Db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Opportunity updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> TogglePublished(long id, CancellationToken cancellationToken)
    {
        var entity = await Db.Opportunities.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();
        entity.IsPublished = !entity.IsPublished; entity.UpdatedAtUtc = DateTime.UtcNow;
        AddAudit(entity.IsPublished ? "OpportunityPublished" : "OpportunityUnpublished", nameof(Opportunity), id.ToString(), $"Changed publication for {entity.Title}.");
        await Db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Opportunity publication updated.";
        return RedirectToAction(nameof(Index));
    }

    private static void Map(OpportunityInputModel model, Opportunity entity)
    {
        entity.Type = model.Type; entity.Title = model.Title.Trim(); entity.Description = model.Description.Trim();
        entity.Location = Clean(model.Location); entity.RegistrationUrl = Clean(model.RegistrationUrl);
        entity.StartsAtUtc = AsUtc(model.StartsAtUtc); entity.EndsAtUtc = model.EndsAtUtc.HasValue ? AsUtc(model.EndsAtUtc.Value) : null;
        entity.RegistrationDeadlineUtc = model.RegistrationDeadlineUtc.HasValue ? AsUtc(model.RegistrationDeadlineUtc.Value) : null;
        entity.Capacity = model.Capacity; entity.IsPublished = model.IsPublished; entity.IsActive = model.IsActive;
    }

    private static DateTime AsUtc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
