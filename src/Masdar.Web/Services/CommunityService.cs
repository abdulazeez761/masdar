using System.Data;
using Masdar.Web.Data;
using Masdar.Web.Domain.Entities;
using Masdar.Web.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Masdar.Web.Services;

public sealed class CommunityService(
    ApplicationDbContext dbContext,
    IAuditService auditService) : ICommunityService
{
    private static readonly int[] SupportedBounties = [20, 40, 60, 80];

    public async Task<PagedResult<CommunityRequestItem>> SearchRequestsAsync(
        CommunityRequestQuery query,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 60);
        var requests = dbContext.ResourceRequests.AsNoTracking().AsQueryable();

        if (query.CourseId is not null)
        {
            requests = requests.Where(x => x.CourseId == query.CourseId);
        }

        if (query.Type is not null)
        {
            requests = requests.Where(x => x.DesiredResourceType == query.Type);
        }

        if (query.Status is not null)
        {
            requests = requests.Where(x => x.Status == query.Status);
        }

        var search = query.Search?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            if (search.Length > 100)
            {
                throw new DomainValidationException("Search text cannot exceed 100 characters.");
            }

            requests = requests.Where(x =>
                x.Title.Contains(search) ||
                (x.Description != null && x.Description.Contains(search)) ||
                x.Course.Code.Contains(search) ||
                x.Course.Name.Contains(search));
        }

        var total = await requests.CountAsync(cancellationToken);
        var items = await requests
            .OrderByDescending(x => x.Votes.Count)
            .ThenByDescending(x => x.BountyPoints)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new CommunityRequestItem(
                x.Id,
                x.CourseId,
                x.Course.Code,
                x.Course.Name,
                x.DesiredResourceType,
                x.Title,
                x.Description,
                x.RequesterId,
                x.Requester.DisplayName,
                x.Votes.Count,
                x.BountyPoints,
                x.Status,
                userId != null && x.Votes.Any(vote => vote.UserId == userId),
                x.FulfilledByResourceId,
                x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<CommunityRequestItem>(items, page, pageSize, total);
    }

    public async Task<long> CreateRequestAsync(
        CreateResourceRequestCommand command,
        string requesterId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        requesterId = RequireUserId(requesterId);
        ValidateRequest(command);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var requester = await dbContext.Users.SingleOrDefaultAsync(x => x.Id == requesterId && x.IsActive, cancellationToken)
            ?? throw new DomainValidationException("The requester account is unavailable.");
        if (!await dbContext.Courses.AnyAsync(x => x.Id == command.CourseId && x.IsActive, cancellationToken))
        {
            throw new DomainValidationException("Select an active course.");
        }

        if (requester.ReputationBalance < command.BountyPoints)
        {
            throw new DomainValidationException("You do not have enough reputation for that bounty.");
        }

        var now = DateTime.UtcNow;
        var request = new ResourceRequest
        {
            CourseId = command.CourseId,
            RequesterId = requesterId,
            DesiredResourceType = command.DesiredResourceType,
            Title = command.Title.Trim(),
            Description = NullIfWhiteSpace(command.Description),
            BountyPoints = command.BountyPoints,
            Status = ResourceRequestStatus.Open,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        dbContext.ResourceRequests.Add(request);
        requester.ReputationBalance -= command.BountyPoints;
        await dbContext.SaveChangesAsync(cancellationToken);

        dbContext.ReputationTransactions.Add(new ReputationTransaction
        {
            UserId = requesterId,
            Type = ReputationTransactionType.RequestBountyReserved,
            Points = -command.BountyPoints,
            BalanceAfter = requester.ReputationBalance,
            Reason = "Reserved a resource-request bounty.",
            IdempotencyKey = $"request-bounty-reserved:{request.Id}",
            RelatedEntityType = nameof(ResourceRequest),
            RelatedEntityId = request.Id.ToString(),
            CreatedAtUtc = now
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync(new AuditEntry(
            "ResourceRequest.Created",
            nameof(ResourceRequest),
            request.Id.ToString(),
            requesterId,
            "A community resource request was created.",
            new { request.CourseId, request.DesiredResourceType, request.BountyPoints }), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return request.Id;
    }

    public async Task<RequestVoteResult> ToggleRequestVoteAsync(
        long requestId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        userId = RequireUserId(userId);
        var requestIsOpen = await dbContext.ResourceRequests
            .AnyAsync(x => x.Id == requestId && x.Status == ResourceRequestStatus.Open, cancellationToken);
        if (!requestIsOpen)
        {
            throw new KeyNotFoundException("The open request was not found.");
        }

        var existing = await dbContext.ResourceRequestVotes
            .SingleOrDefaultAsync(x => x.ResourceRequestId == requestId && x.UserId == userId, cancellationToken);
        bool isActive;
        if (existing is null)
        {
            dbContext.ResourceRequestVotes.Add(new ResourceRequestVote { ResourceRequestId = requestId, UserId = userId });
            isActive = true;
        }
        else
        {
            dbContext.ResourceRequestVotes.Remove(existing);
            isActive = false;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        var count = await dbContext.ResourceRequestVotes.CountAsync(x => x.ResourceRequestId == requestId, cancellationToken);
        return new RequestVoteResult(isActive, count);
    }

    public async Task<RequestFulfillmentResult> FulfillRequestAsync(
        long requestId,
        long resourceId,
        string contributorId,
        CancellationToken cancellationToken = default)
    {
        contributorId = RequireUserId(contributorId);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var request = await dbContext.ResourceRequests.SingleOrDefaultAsync(x => x.Id == requestId, cancellationToken)
            ?? throw new KeyNotFoundException("The resource request was not found.");
        var contributor = await dbContext.Users.SingleOrDefaultAsync(x => x.Id == contributorId && x.IsActive, cancellationToken)
            ?? throw new DomainValidationException("The contributor account is unavailable.");
        var resource = await dbContext.StudyResources.SingleOrDefaultAsync(x => x.Id == resourceId, cancellationToken)
            ?? throw new KeyNotFoundException("The resource was not found.");

        if (request.Status == ResourceRequestStatus.Fulfilled && request.FulfilledByResourceId == resourceId && resource.UploaderId == contributorId)
        {
            await transaction.CommitAsync(cancellationToken);
            return new RequestFulfillmentResult(requestId, resourceId, request.BountyPoints, contributor.ReputationBalance);
        }

        if (request.Status != ResourceRequestStatus.Open)
        {
            throw new DomainValidationException("This request is no longer open.");
        }

        if (request.RequesterId == contributorId)
        {
            throw new DomainValidationException("You cannot claim the bounty on your own request.");
        }

        if (resource.UploaderId != contributorId ||
            resource.PublicationStatus != ResourcePublicationStatus.Published ||
            resource.FileScanStatus != FileScanStatus.Validated)
        {
            throw new DomainValidationException("Only your published, validated resource can fulfill a request.");
        }

        if (resource.CourseId != request.CourseId || resource.Type != request.DesiredResourceType)
        {
            throw new DomainValidationException("The resource course and type must match the request.");
        }

        var idempotencyKey = $"request-bounty-awarded:{request.Id}";
        if (await dbContext.ReputationTransactions.AnyAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken))
        {
            throw new DomainValidationException("This request bounty has already been awarded.");
        }

        var now = DateTime.UtcNow;
        request.Status = ResourceRequestStatus.Fulfilled;
        request.FulfilledByResourceId = resourceId;
        request.FulfilledAtUtc = now;
        request.UpdatedAtUtc = now;
        contributor.ReputationBalance += request.BountyPoints;
        dbContext.ReputationTransactions.Add(new ReputationTransaction
        {
            UserId = contributorId,
            Type = ReputationTransactionType.RequestBountyAwarded,
            Points = request.BountyPoints,
            BalanceAfter = contributor.ReputationBalance,
            Reason = "Fulfilled a community resource request.",
            IdempotencyKey = idempotencyKey,
            RelatedEntityType = nameof(ResourceRequest),
            RelatedEntityId = request.Id.ToString(),
            CreatedAtUtc = now
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync(new AuditEntry(
            "ResourceRequest.Fulfilled",
            nameof(ResourceRequest),
            request.Id.ToString(),
            contributorId,
            "A community resource request was fulfilled.",
            new { ResourceId = resourceId, AwardedPoints = request.BountyPoints }), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new RequestFulfillmentResult(request.Id, resource.Id, request.BountyPoints, contributor.ReputationBalance);
    }

    public async Task<IReadOnlyList<LeaderboardItem>> GetLeaderboardAsync(
        int take = 20,
        CancellationToken cancellationToken = default) =>
        await dbContext.Users
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.ReputationBalance)
            .ThenByDescending(x => x.UploadedResources.Count(resource => resource.PublicationStatus == ResourcePublicationStatus.Published))
            .ThenBy(x => x.DisplayName)
            .Take(Math.Clamp(take, 1, 100))
            .Select(x => new LeaderboardItem(
                x.Id,
                x.DisplayName,
                x.Major == null ? null : x.Major.Name,
                x.UploadedResources.Count(resource => resource.PublicationStatus == ResourcePublicationStatus.Published),
                x.ReputationBalance))
            .ToListAsync(cancellationToken);

    private static void ValidateRequest(CreateResourceRequestCommand command)
    {
        var titleLength = command.Title?.Trim().Length ?? 0;
        if (titleLength is < 4 or > 220)
        {
            throw new DomainValidationException("Request title must be between 4 and 220 characters.");
        }

        if (command.Description?.Trim().Length > 3000)
        {
            throw new DomainValidationException("Request description cannot exceed 3000 characters.");
        }

        if (!SupportedBounties.Contains(command.BountyPoints))
        {
            throw new DomainValidationException("Choose a supported bounty: 20, 40, 60, or 80 points.");
        }
    }

    private static string RequireUserId(string? userId) =>
        string.IsNullOrWhiteSpace(userId) ? throw new UnauthorizedAccessException("An authenticated user is required.") : userId;

    private static string? NullIfWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
