using System.Data;
using Masdar.Web.Data;
using Masdar.Web.Domain.Entities;
using Masdar.Web.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Masdar.Web.Services;

public sealed class ResourceService(
    ApplicationDbContext dbContext,
    IFileStorageService fileStorage,
    IAuditService auditService) : IResourceService
{
    private const int PublishAward = 15;

    public async Task<PagedResult<ResourceListItem>> SearchAsync(
        ResourceQuery query,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var (page, pageSize) = NormalizePage(query.Page, query.PageSize, 60);
        var resources = VisibleResources();

        if (query.UniversityId is not null)
        {
            resources = resources.Where(x => x.Course.Major.UniversityId == query.UniversityId);
        }

        if (query.MajorId is not null)
        {
            resources = resources.Where(x => x.Course.MajorId == query.MajorId);
        }

        if (query.CourseIds is { Count: > 0 })
        {
            var courseIds = query.CourseIds.Where(x => x > 0).Distinct().Take(50).ToArray();
            resources = resources.Where(x => courseIds.Contains(x.CourseId));
        }

        if (query.Type is not null)
        {
            resources = resources.Where(x => x.Type == query.Type);
        }

        var semester = query.Semester?.Trim();
        if (!string.IsNullOrWhiteSpace(semester))
        {
            if (semester.Length > 40)
            {
                throw new DomainValidationException("Semester cannot exceed 40 characters.");
            }

            resources = resources.Where(x => x.Semester == semester);
        }

        if (query.SavedOnly)
        {
            resources = string.IsNullOrWhiteSpace(userId)
                ? resources.Where(_ => false)
                : resources.Where(x => x.SavedByUsers.Any(saved => saved.UserId == userId));
        }

        var search = query.Search?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            if (search.Length > 100)
            {
                throw new DomainValidationException("Search text cannot exceed 100 characters.");
            }

            resources = resources.Where(x =>
                x.Title.Contains(search) ||
                x.Description.Contains(search) ||
                x.Course.Code.Contains(search) ||
                x.Course.Name.Contains(search) ||
                x.Uploader.DisplayName.Contains(search) ||
                x.ResourceTags.Any(tag => tag.Tag.Name.Contains(search)));
        }

        var total = await resources.CountAsync(cancellationToken);
        resources = query.Sort switch
        {
            ResourceSort.MostHelpful => resources
                .OrderByDescending(x => x.HelpfulVotes.Count)
                .ThenByDescending(x => x.PublishedAtUtc)
                .ThenByDescending(x => x.Id),
            ResourceSort.Newest => resources
                .OrderByDescending(x => x.PublishedAtUtc)
                .ThenByDescending(x => x.Id),
            ResourceSort.Title => resources
                .OrderBy(x => x.Title)
                .ThenBy(x => x.Id),
            _ => resources
                .OrderByDescending(x => x.Ratings.Select(r => (double?)r.Value).Average() ?? 0)
                .ThenByDescending(x => x.Ratings.Count)
                .ThenByDescending(x => x.HelpfulVotes.Count)
                .ThenByDescending(x => x.Id)
        };

        var items = await resources
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ResourceListItem(
                x.Id,
                x.CourseId,
                x.Course.Code,
                x.Course.Name,
                x.Title,
                x.Type,
                x.Semester,
                x.Uploader.DisplayName,
                x.Ratings.Select(r => (double?)r.Value).Average() ?? 0,
                x.Ratings.Count,
                x.HelpfulVotes.Count,
                x.VerificationStatus == ResourceVerificationStatus.Verified,
                x.IsRepresentative,
                userId != null && x.SavedByUsers.Any(saved => saved.UserId == userId),
                x.ResourceTags.OrderBy(tag => tag.Tag.Name).Select(tag => tag.Tag.Name).ToList(),
                x.PublishedAtUtc ?? x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<ResourceListItem>(items, page, pageSize, total);
    }

    public Task<ResourceDetails?> GetDetailsAsync(
        long resourceId,
        string? userId,
        CancellationToken cancellationToken = default) =>
        VisibleResources()
            .Where(x => x.Id == resourceId)
            .Select(x => new ResourceDetails(
                x.Id,
                x.CourseId,
                x.Course.Code,
                x.Course.Name,
                x.Title,
                x.Type,
                x.Semester,
                x.Description,
                x.TutorContext,
                x.UploaderId,
                x.Uploader.DisplayName,
                x.Ratings.Select(r => (double?)r.Value).Average() ?? 0,
                x.Ratings.Count,
                x.HelpfulVotes.Count,
                userId == null
                    ? null
                    : x.Ratings.Where(rating => rating.UserId == userId).Select(rating => (int?)rating.Value).FirstOrDefault(),
                userId != null && x.HelpfulVotes.Any(vote => vote.UserId == userId),
                userId != null && x.SavedByUsers.Any(saved => saved.UserId == userId),
                x.VerificationStatus == ResourceVerificationStatus.Verified,
                x.IsRepresentative,
                x.OriginalFileName,
                x.ContentType,
                x.FileSizeBytes,
                x.ResourceTags.OrderBy(tag => tag.Tag.Name).Select(tag => tag.Tag.Name).ToList(),
                x.PublishedAtUtc ?? x.CreatedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<ResourceDownload?> OpenDownloadAsync(
        long resourceId,
        CancellationToken cancellationToken = default)
    {
        var metadata = await VisibleResources()
            .Where(x => x.Id == resourceId)
            .Select(x => new { x.StorageKey, x.ContentType, x.OriginalFileName })
            .SingleOrDefaultAsync(cancellationToken);
        if (metadata is null)
        {
            return null;
        }

        var content = await fileStorage.OpenReadAsync(metadata.StorageKey, cancellationToken);
        return new ResourceDownload(content, metadata.ContentType, metadata.OriginalFileName);
    }

    public async Task<ResourceInteractionResult> ToggleSaveAsync(
        long resourceId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        await RequireVisibleResourceAsync(resourceId, cancellationToken);
        var existing = await dbContext.SavedResources.FindAsync([userId, resourceId], cancellationToken);
        bool isActive;
        if (existing is null)
        {
            dbContext.SavedResources.Add(new SavedResource { ResourceId = resourceId, UserId = RequireUserId(userId) });
            isActive = true;
        }
        else
        {
            dbContext.SavedResources.Remove(existing);
            isActive = false;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        var count = await dbContext.SavedResources.CountAsync(x => x.ResourceId == resourceId, cancellationToken);
        return new ResourceInteractionResult(isActive, count);
    }

    public async Task<ResourceInteractionResult> ToggleHelpfulAsync(
        long resourceId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        await RequireVisibleResourceAsync(resourceId, cancellationToken);
        userId = RequireUserId(userId);
        var existing = await dbContext.ResourceHelpfulVotes
            .SingleOrDefaultAsync(x => x.ResourceId == resourceId && x.UserId == userId, cancellationToken);
        bool isActive;
        if (existing is null)
        {
            dbContext.ResourceHelpfulVotes.Add(new ResourceHelpfulVote { ResourceId = resourceId, UserId = userId });
            isActive = true;
        }
        else
        {
            dbContext.ResourceHelpfulVotes.Remove(existing);
            isActive = false;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        var count = await dbContext.ResourceHelpfulVotes.CountAsync(x => x.ResourceId == resourceId, cancellationToken);
        return new ResourceInteractionResult(isActive, count);
    }

    public async Task<ResourceRatingResult> RateAsync(
        long resourceId,
        string userId,
        int value,
        CancellationToken cancellationToken = default)
    {
        if (value is < 1 or > 5)
        {
            throw new DomainValidationException("Rating must be between 1 and 5.");
        }

        await RequireVisibleResourceAsync(resourceId, cancellationToken);
        userId = RequireUserId(userId);
        var now = DateTime.UtcNow;
        var rating = await dbContext.ResourceRatings
            .SingleOrDefaultAsync(x => x.ResourceId == resourceId && x.UserId == userId, cancellationToken);
        if (rating is null)
        {
            dbContext.ResourceRatings.Add(new ResourceRating
            {
                ResourceId = resourceId,
                UserId = userId,
                Value = value,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }
        else
        {
            rating.Value = value;
            rating.UpdatedAtUtc = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        var summary = await dbContext.ResourceRatings
            .Where(x => x.ResourceId == resourceId)
            .GroupBy(_ => 1)
            .Select(group => new { Count = group.Count(), Average = group.Average(x => x.Value) })
            .SingleAsync(cancellationToken);
        return new ResourceRatingResult(value, summary.Average, summary.Count);
    }

    public async Task<long> CreateAsync(
        CreateResourceCommand command,
        string uploaderId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        uploaderId = RequireUserId(uploaderId);
        ValidateCreateCommand(command);

        var courseExists = await dbContext.Courses.AnyAsync(x => x.Id == command.CourseId && x.IsActive, cancellationToken);
        if (!courseExists)
        {
            throw new DomainValidationException("Select an active course.");
        }

        var storedFile = await fileStorage.SaveValidatedAsync(command.File, cancellationToken);

        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var user = await dbContext.Users.SingleOrDefaultAsync(x => x.Id == uploaderId && x.IsActive, cancellationToken)
                ?? throw new DomainValidationException("The contributor account is unavailable.");
            var now = DateTime.UtcNow;
            var resource = new StudyResource
            {
                CourseId = command.CourseId,
                UploaderId = uploaderId,
                Title = command.Title.Trim(),
                Type = command.Type,
                Semester = command.Semester.Trim(),
                Description = command.Description.Trim(),
                TutorContext = NullIfWhiteSpace(command.TutorContext),
                PublicationStatus = ResourcePublicationStatus.Published,
                VerificationStatus = ResourceVerificationStatus.Unverified,
                FileScanStatus = FileScanStatus.Validated,
                StorageKey = storedFile.StorageKey,
                OriginalFileName = storedFile.OriginalFileName,
                ContentType = storedFile.ContentType,
                FileSizeBytes = storedFile.SizeBytes,
                Sha256 = storedFile.Sha256,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                PublishedAtUtc = now
            };

            dbContext.StudyResources.Add(resource);
            await AttachTagsAsync(resource, command.Tags, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            var idempotencyKey = $"resource-published:{resource.Id}";
            if (!await dbContext.ReputationTransactions.AnyAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken))
            {
                user.ReputationBalance += PublishAward;
                dbContext.ReputationTransactions.Add(new ReputationTransaction
                {
                    UserId = uploaderId,
                    Type = ReputationTransactionType.ResourcePublished,
                    Points = PublishAward,
                    BalanceAfter = user.ReputationBalance,
                    Reason = "Published a study resource.",
                    IdempotencyKey = idempotencyKey,
                    RelatedEntityType = nameof(StudyResource),
                    RelatedEntityId = resource.Id.ToString(),
                    CreatedAtUtc = now
                });
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await auditService.WriteAsync(new AuditEntry(
                "Resource.Created",
                nameof(StudyResource),
                resource.Id.ToString(),
                uploaderId,
                "A study resource was published.",
                new { resource.CourseId, resource.Type, storedFile.SizeBytes }), cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return resource.Id;
        }
        catch
        {
            await fileStorage.DeleteAsync(storedFile.StorageKey, CancellationToken.None);
            throw;
        }
    }

    private IQueryable<StudyResource> VisibleResources() => dbContext.StudyResources
        .AsNoTracking()
        .Where(x =>
            x.PublicationStatus == ResourcePublicationStatus.Published &&
            x.FileScanStatus == FileScanStatus.Validated &&
            x.Course.IsActive &&
            x.Course.Major.IsActive &&
            x.Course.Major.University.IsActive);

    private async Task RequireVisibleResourceAsync(long resourceId, CancellationToken cancellationToken)
    {
        if (resourceId <= 0 || !await VisibleResources().AnyAsync(x => x.Id == resourceId, cancellationToken))
        {
            throw new KeyNotFoundException("The resource was not found.");
        }
    }

    private async Task AttachTagsAsync(
        StudyResource resource,
        IReadOnlyCollection<string>? tagValues,
        CancellationToken cancellationToken)
    {
        var tagPairs = (tagValues ?? [])
            .Select(x => x?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => new { Name = x!, Normalized = x!.ToUpperInvariant() })
            .DistinctBy(x => x.Normalized)
            .ToArray();
        if (tagPairs.Length > 8)
        {
            throw new DomainValidationException("A resource can have at most 8 tags.");
        }

        if (tagPairs.Any(x => x.Name.Length > 60))
        {
            throw new DomainValidationException("Tags cannot exceed 60 characters.");
        }

        var normalizedNames = tagPairs.Select(x => x.Normalized).ToArray();
        var existing = await dbContext.Tags
            .Where(x => normalizedNames.Contains(x.NormalizedName))
            .ToDictionaryAsync(x => x.NormalizedName, cancellationToken);

        foreach (var pair in tagPairs)
        {
            if (!existing.TryGetValue(pair.Normalized, out var tag))
            {
                tag = new Tag { Name = pair.Name, NormalizedName = pair.Normalized };
                dbContext.Tags.Add(tag);
                existing[pair.Normalized] = tag;
            }

            resource.ResourceTags.Add(new ResourceTag { Resource = resource, Tag = tag });
        }
    }

    private static void ValidateCreateCommand(CreateResourceCommand command)
    {
        if (!command.HasSharingPermission)
        {
            throw new DomainValidationException("Confirm that you have permission to share this file.");
        }

        ValidateLength(command.Title, "Title", 4, 220);
        ValidateLength(command.Semester, "Semester", 2, 40);
        ValidateLength(command.Description, "Description", 10, 6000);
        if (command.TutorContext?.Trim().Length > 6000)
        {
            throw new DomainValidationException("Tutor context cannot exceed 6000 characters.");
        }
    }

    private static void ValidateLength(string? value, string field, int minimum, int maximum)
    {
        var length = value?.Trim().Length ?? 0;
        if (length < minimum || length > maximum)
        {
            throw new DomainValidationException($"{field} must be between {minimum} and {maximum} characters.");
        }
    }

    private static (int Page, int PageSize) NormalizePage(int page, int pageSize, int maximumPageSize) =>
        (Math.Max(1, page), Math.Clamp(pageSize, 1, maximumPageSize));

    private static string RequireUserId(string? userId) =>
        string.IsNullOrWhiteSpace(userId) ? throw new UnauthorizedAccessException("An authenticated user is required.") : userId;

    private static string? NullIfWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
