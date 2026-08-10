using Masdar.Web.Areas.Admin.ViewModels;
using Masdar.Web.Data;
using Masdar.Web.Domain.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Masdar.Web.Areas.Admin.Controllers;

public sealed class CoursesController(ApplicationDbContext db) : AdminControllerBase(db)
{
    public async Task<IActionResult> Index(string? query, int? majorId, CancellationToken cancellationToken)
    {
        var items = Db.Courses.AsNoTracking().Include(x => x.Major).ThenInclude(x => x.University).Include(x => x.Resources).AsQueryable();
        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            items = items.Where(x => x.Code.Contains(term) || x.Name.Contains(term) || x.Instructor.Contains(term));
        }
        if (majorId.HasValue) items = items.Where(x => x.MajorId == majorId);
        ViewBag.Query = query; ViewBag.MajorId = majorId;
        ViewBag.Majors = await Db.Majors.AsNoTracking().Include(x => x.University).OrderBy(x => x.University.Name).ThenBy(x => x.Name).ToListAsync(cancellationToken);
        return View(await items.OrderBy(x => x.Code).Take(300).ToListAsync(cancellationToken));
    }

    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = new CourseInputModel { Semester = "Spring 2026", IsActive = true };
        await LoadOptions(model, cancellationToken);
        return View("Edit", model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CourseInputModel model, CancellationToken cancellationToken)
    {
        await Validate(model, null, cancellationToken);
        if (!ModelState.IsValid) { await LoadOptions(model, cancellationToken); return View("Edit", model); }
        var entity = new Course
        {
            MajorId = model.MajorId, Code = model.Code.Trim().ToUpperInvariant(), Name = model.Name.Trim(), Instructor = model.Instructor.Trim(),
            Semester = model.Semester.Trim(), ExamDateUtc = NormalizeDate(model.ExamDateUtc), Description = Clean(model.Description), IsActive = model.IsActive
        };
        Db.Courses.Add(entity); AddAudit("CourseCreated", nameof(Course), null, $"Created course {entity.Code}.");
        await Db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Course created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var entity = await Db.Courses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();
        var model = new CourseInputModel
        {
            Id = entity.Id, MajorId = entity.MajorId, Code = entity.Code, Name = entity.Name, Instructor = entity.Instructor,
            Semester = entity.Semester, ExamDateUtc = entity.ExamDateUtc, Description = entity.Description, IsActive = entity.IsActive
        };
        await LoadOptions(model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, CourseInputModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id) return BadRequest();
        var entity = await Db.Courses.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();
        await Validate(model, id, cancellationToken);
        if (!ModelState.IsValid) { await LoadOptions(model, cancellationToken); return View(model); }
        entity.MajorId = model.MajorId; entity.Code = model.Code.Trim().ToUpperInvariant(); entity.Name = model.Name.Trim(); entity.Instructor = model.Instructor.Trim();
        entity.Semester = model.Semester.Trim(); entity.ExamDateUtc = NormalizeDate(model.ExamDateUtc); entity.Description = Clean(model.Description);
        entity.IsActive = model.IsActive; entity.UpdatedAtUtc = DateTime.UtcNow;
        AddAudit("CourseUpdated", nameof(Course), id.ToString(), $"Updated course {entity.Code}.");
        await Db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Course updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Toggle(int id, CancellationToken cancellationToken)
    {
        var entity = await Db.Courses.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();
        entity.IsActive = !entity.IsActive; entity.UpdatedAtUtc = DateTime.UtcNow;
        AddAudit(entity.IsActive ? "CourseActivated" : "CourseDeactivated", nameof(Course), id.ToString(), $"Changed status for {entity.Code}.");
        await Db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Course status updated.";
        return RedirectToAction(nameof(Index));
    }

    private async Task Validate(CourseInputModel model, int? excludedId, CancellationToken cancellationToken)
    {
        if (!await Db.Majors.AnyAsync(x => x.Id == model.MajorId, cancellationToken)) ModelState.AddModelError(nameof(model.MajorId), "Select a valid major.");
        var code = model.Code.Trim().ToUpperInvariant();
        if (await Db.Courses.AnyAsync(x => x.MajorId == model.MajorId && x.Code == code && (!excludedId.HasValue || x.Id != excludedId), cancellationToken))
            ModelState.AddModelError(nameof(model.Code), "This course code already exists in the selected major.");
    }

    private async Task LoadOptions(CourseInputModel model, CancellationToken cancellationToken)
    {
        var majors = await Db.Majors.AsNoTracking().Include(x => x.University).OrderBy(x => x.University.Name).ThenBy(x => x.Name).ToListAsync(cancellationToken);
        model.MajorOptions = majors.Select(x => new SelectListItem($"{x.University.Name} — {x.Name}", x.Id.ToString())).ToList();
    }

    private static DateTime? NormalizeDate(DateTime? value) => value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : null;
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
