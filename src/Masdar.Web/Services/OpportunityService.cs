using System.Data;
using Masdar.Web.Data;
using Masdar.Web.Domain.Entities;
using Masdar.Web.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Masdar.Web.Services;

public sealed class OpportunityService(
    ApplicationDbContext dbContext,
    IAuditService auditService) : IOpportunityService
{
    public async Task<IReadOnlyList<OpportunityItem>> ListAsync(
        string? userId,
        CancellationToken cancellationToken = default) =>
        await dbContext.Opportunities
            .AsNoTracking()
            .Where(x => x.IsPublished && x.IsActive)
            .OrderBy(x => x.StartsAtUtc)
            .ThenBy(x => x.Id)
            .Select(x => new OpportunityItem(
                x.Id,
                x.Type,
                x.Title,
                x.Description,
                x.Location,
                x.RegistrationUrl,
                x.StartsAtUtc,
                x.EndsAtUtc,
                x.RegistrationDeadlineUtc,
                x.Capacity,
                x.Registrations.Count(registration =>
                    registration.Status == OpportunityRegistrationStatus.Registered ||
                    registration.Status == OpportunityRegistrationStatus.Attended),
                userId != null && x.Registrations.Any(registration =>
                    registration.UserId == userId &&
                    (registration.Status == OpportunityRegistrationStatus.Registered ||
                     registration.Status == OpportunityRegistrationStatus.Waitlisted ||
                     registration.Status == OpportunityRegistrationStatus.Attended))))
            .ToListAsync(cancellationToken);

    public async Task<OpportunityRegistrationResult> ToggleRegistrationAsync(
        long opportunityId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        userId = RequireUserId(userId);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var opportunity = await dbContext.Opportunities
            .SingleOrDefaultAsync(x => x.Id == opportunityId && x.IsPublished && x.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("The opportunity was not found.");
        var registration = await dbContext.OpportunityRegistrations
            .SingleOrDefaultAsync(x => x.OpportunityId == opportunityId && x.UserId == userId, cancellationToken);
        var now = DateTime.UtcNow;
        bool isRegistered;

        if (registration is not null && registration.Status is OpportunityRegistrationStatus.Registered or OpportunityRegistrationStatus.Waitlisted)
        {
            registration.Status = OpportunityRegistrationStatus.Cancelled;
            registration.UpdatedAtUtc = now;
            isRegistered = false;
        }
        else
        {
            if (opportunity.RegistrationDeadlineUtc is not null && opportunity.RegistrationDeadlineUtc < now)
            {
                throw new DomainValidationException("Registration for this opportunity has closed.");
            }

            var registeredCount = await dbContext.OpportunityRegistrations.CountAsync(x =>
                x.OpportunityId == opportunityId &&
                (x.Status == OpportunityRegistrationStatus.Registered || x.Status == OpportunityRegistrationStatus.Attended), cancellationToken);
            var status = opportunity.Capacity is not null && registeredCount >= opportunity.Capacity
                ? OpportunityRegistrationStatus.Waitlisted
                : OpportunityRegistrationStatus.Registered;
            if (registration is null)
            {
                dbContext.OpportunityRegistrations.Add(new OpportunityRegistration
                {
                    OpportunityId = opportunityId,
                    UserId = userId,
                    Status = status,
                    RegisteredAtUtc = now,
                    UpdatedAtUtc = now
                });
            }
            else
            {
                registration.Status = status;
                registration.RegisteredAtUtc = now;
                registration.UpdatedAtUtc = now;
            }

            isRegistered = true;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync(new AuditEntry(
            isRegistered ? "Opportunity.Registered" : "Opportunity.RegistrationCancelled",
            nameof(Opportunity),
            opportunityId.ToString(),
            userId), cancellationToken);
        var count = await dbContext.OpportunityRegistrations.CountAsync(x =>
            x.OpportunityId == opportunityId &&
            (x.Status == OpportunityRegistrationStatus.Registered || x.Status == OpportunityRegistrationStatus.Attended), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new OpportunityRegistrationResult(isRegistered, count);
    }

    private static string RequireUserId(string? userId) =>
        string.IsNullOrWhiteSpace(userId) ? throw new UnauthorizedAccessException("An authenticated user is required.") : userId;
}
