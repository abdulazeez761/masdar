using Masdar.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace Masdar.Web.Areas.Admin.Controllers;

public sealed class ActivityLogsController(ApplicationDbContext db) : AdminControllerBase(db)
{
    public async Task<IActionResult> Index(string? query, CancellationToken cancellationToken)
    {
        var items = Db.ActivityLogs.AsNoTracking().Include(x => x.ActorUser).AsQueryable();
        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            items = items.Where(x => x.Action.Contains(term) || x.EntityType.Contains(term) || (x.Description != null && x.Description.Contains(term)));
        }
        ViewBag.Query = query;
        return View(await items.OrderByDescending(x => x.CreatedAtUtc).Take(500).ToListAsync(cancellationToken));
    }
}
