using Masdar.Web.Domain.Enums;

namespace Masdar.Web.Services;

public sealed record CommunityRequestQuery(
    string? Search = null,
    int? CourseId = null,
    ResourceType? Type = null,
    ResourceRequestStatus? Status = ResourceRequestStatus.Open,
    int Page = 1,
    int PageSize = 20);

public sealed record CommunityRequestItem(
    long Id,
    int CourseId,
    string CourseCode,
    string CourseName,
    ResourceType DesiredResourceType,
    string Title,
    string? Description,
    string RequesterId,
    string RequesterName,
    int VoteCount,
    int BountyPoints,
    ResourceRequestStatus Status,
    bool HasCurrentUserVoted,
    long? FulfilledByResourceId,
    DateTime CreatedAtUtc);

public sealed record CreateResourceRequestCommand(
    int CourseId,
    ResourceType DesiredResourceType,
    string Title,
    string? Description,
    int BountyPoints);

public sealed record RequestVoteResult(bool IsActive, int VoteCount);
public sealed record RequestFulfillmentResult(long RequestId, long ResourceId, int AwardedPoints, int ContributorBalance);
public sealed record LeaderboardItem(string UserId, string DisplayName, string? MajorName, int UploadCount, int ReputationBalance);

public interface ICommunityService
{
    Task<PagedResult<CommunityRequestItem>> SearchRequestsAsync(CommunityRequestQuery query, string? userId, CancellationToken cancellationToken = default);
    Task<long> CreateRequestAsync(CreateResourceRequestCommand command, string requesterId, CancellationToken cancellationToken = default);
    Task<RequestVoteResult> ToggleRequestVoteAsync(long requestId, string userId, CancellationToken cancellationToken = default);
    Task<RequestFulfillmentResult> FulfillRequestAsync(long requestId, long resourceId, string contributorId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaderboardItem>> GetLeaderboardAsync(int take = 20, CancellationToken cancellationToken = default);
}
