using System.Security.Claims;
using Masdar.Web.Data;
using Masdar.Web.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Masdar.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
[AutoValidateAntiforgeryToken]
public abstract class AdminControllerBase(ApplicationDbContext db) : Controller
{
    protected ApplicationDbContext Db { get; } = db;

    protected void AddAudit(string action, string entityType, string? entityId, string description)
    {
        Db.ActivityLogs.Add(new ActivityLog
        {
            ActorUserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Description = description,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString(),
            CorrelationId = HttpContext.TraceIdentifier
        });
    }
}
