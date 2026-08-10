using System.Text.Json;
using Masdar.Web.Data;
using Masdar.Web.Domain.Entities;

namespace Masdar.Web.Services;

public sealed class AuditService(
    ApplicationDbContext dbContext,
    IHttpContextAccessor? httpContextAccessor = null) : IAuditService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task WriteAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (string.IsNullOrWhiteSpace(entry.Action) || string.IsNullOrWhiteSpace(entry.EntityType))
        {
            throw new ArgumentException("Audit action and entity type are required.", nameof(entry));
        }

        var httpContext = httpContextAccessor?.HttpContext;
        var log = new ActivityLog
        {
            Action = entry.Action.Trim(),
            EntityType = entry.EntityType.Trim(),
            EntityId = entry.EntityId?.Trim(),
            ActorUserId = entry.ActorUserId,
            Description = entry.Description?.Trim(),
            MetadataJson = entry.Metadata is null ? null : JsonSerializer.Serialize(entry.Metadata, SerializerOptions),
            IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Truncate(httpContext?.Request.Headers.UserAgent.ToString(), 512),
            CorrelationId = httpContext?.TraceIdentifier,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.ActivityLogs.Add(log);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string? Truncate(string? value, int maximumLength) =>
        string.IsNullOrWhiteSpace(value) ? null : value[..Math.Min(value.Length, maximumLength)];
}
