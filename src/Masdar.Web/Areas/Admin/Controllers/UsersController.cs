using System.Security.Claims;
using Masdar.Web.Areas.Admin.ViewModels;
using Masdar.Web.Data;
using Masdar.Web.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Masdar.Web.Areas.Admin.Controllers;

public sealed class UsersController(ApplicationDbContext db, UserManager<ApplicationUser> userManager) : AdminControllerBase(db)
{
    public async Task<IActionResult> Index(string? query, CancellationToken cancellationToken)
    {
        var usersQuery = Db.Users.AsNoTracking().Include(x => x.University).Include(x => x.Major).AsQueryable();
        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            usersQuery = usersQuery.Where(x => x.DisplayName.Contains(term) || (x.Email != null && x.Email.Contains(term)));
        }

        var users = await usersQuery.OrderBy(x => x.DisplayName).Take(200).ToListAsync(cancellationToken);
        var rows = new List<AdminUserRowViewModel>(users.Count);
        foreach (var user in users)
            rows.Add(new AdminUserRowViewModel { User = user, Roles = (await userManager.GetRolesAsync(user)).ToArray() });

        ViewBag.Query = query;
        return View(rows);
    }

    public async Task<IActionResult> Details(string id, CancellationToken cancellationToken)
    {
        var user = await Db.Users.AsNoTracking()
            .Include(x => x.University).Include(x => x.Major)
            .Include(x => x.UploadedResources).Include(x => x.ResourceRequests)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user is null) return NotFound();

        ViewBag.Roles = await userManager.GetRolesAsync(user);
        ViewBag.Reputation = await Db.ReputationTransactions.AsNoTracking().Where(x => x.UserId == id)
            .OrderByDescending(x => x.CreatedAtUtc).Take(20).ToListAsync(cancellationToken);
        return View(user);
    }

    [HttpPost]
    public async Task<IActionResult> ToggleActive(string id, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();
        if (User.FindFirstValue(ClaimTypes.NameIdentifier) == id)
        {
            TempData["Error"] = "You cannot deactivate your own administrator account.";
            return RedirectToAction(nameof(Details), new { id });
        }

        user.IsActive = !user.IsActive;
        user.UpdatedAtUtc = DateTime.UtcNow;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            TempData["Error"] = string.Join(" ", result.Errors.Select(x => x.Description));
            return RedirectToAction(nameof(Details), new { id });
        }

        AddAudit(user.IsActive ? "UserActivated" : "UserDeactivated", nameof(ApplicationUser), id, $"{user.Email} was {(user.IsActive ? "activated" : "deactivated")}.");
        await Db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "User status updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> SetRole(string id, string role, CancellationToken cancellationToken)
    {
        if (role is not ("Admin" or "User")) return BadRequest();
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == id && role != "Admin")
        {
            TempData["Error"] = "You cannot remove your own administrator role.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var roles = await userManager.GetRolesAsync(user);
        var remove = roles.Where(x => x is "Admin" or "User" && x != role).ToArray();
        if (remove.Length > 0) await userManager.RemoveFromRolesAsync(user, remove);
        if (!await userManager.IsInRoleAsync(user, role)) await userManager.AddToRoleAsync(user, role);

        AddAudit("UserRoleChanged", nameof(ApplicationUser), id, $"{user.Email} assigned to {role} role.");
        await Db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Role updated.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
