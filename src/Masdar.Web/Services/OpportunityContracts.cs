using Masdar.Web.Domain.Enums;

namespace Masdar.Web.Services;

public sealed record OpportunityItem(
    long Id,
    OpportunityType Type,
    string Title,
    string Description,
    string? Location,
    string? RegistrationUrl,
    DateTime StartsAtUtc,
    DateTime? EndsAtUtc,
    DateTime? RegistrationDeadlineUtc,
    int? Capacity,
    int RegistrationCount,
    bool IsCurrentUserRegistered);

public sealed record OpportunityRegistrationResult(bool IsRegistered, int RegistrationCount);

public interface IOpportunityService
{
    Task<IReadOnlyList<OpportunityItem>> ListAsync(string? userId, CancellationToken cancellationToken = default);
    Task<OpportunityRegistrationResult> ToggleRegistrationAsync(long opportunityId, string userId, CancellationToken cancellationToken = default);
}
