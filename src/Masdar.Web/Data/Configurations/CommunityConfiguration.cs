using Masdar.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Masdar.Web.Data.Configurations;

public sealed class RoadmapNodeConfiguration : IEntityTypeConfiguration<RoadmapNode>
{
    public void Configure(EntityTypeBuilder<RoadmapNode> builder)
    {
        builder.Property(x => x.Title).HasMaxLength(180).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1500).IsRequired();
        builder.Property(x => x.Difficulty).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(x => new { x.CourseId, x.Order }).IsUnique();
        builder.HasOne(x => x.Course).WithMany(x => x.RoadmapNodes).HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Cascade);
        builder.ToTable(t => t.HasCheckConstraint("CK_RoadmapNodes_ExamWeight", "[ExamWeightPercent] BETWEEN 0 AND 100"));
    }
}

public sealed class UserRoadmapProgressConfiguration : IEntityTypeConfiguration<UserRoadmapProgress>
{
    public void Configure(EntityTypeBuilder<UserRoadmapProgress> builder)
    {
        builder.HasKey(x => new { x.UserId, x.RoadmapNodeId });
        builder.HasOne(x => x.User).WithMany(x => x.RoadmapProgress).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.RoadmapNode).WithMany(x => x.UserProgress).HasForeignKey(x => x.RoadmapNodeId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ResourceRequestConfiguration : IEntityTypeConfiguration<ResourceRequest>
{
    public void Configure(EntityTypeBuilder<ResourceRequest> builder)
    {
        builder.Property(x => x.DesiredResourceType).HasConversion<string>().HasMaxLength(40);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Title).HasMaxLength(220).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(3000);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => new { x.Status, x.CourseId, x.CreatedAtUtc });
        builder.HasIndex(x => x.RequesterId);
        builder.HasIndex(x => x.FulfilledByResourceId).HasFilter("[FulfilledByResourceId] IS NOT NULL");
        builder.HasOne(x => x.Course).WithMany(x => x.ResourceRequests).HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Requester).WithMany(x => x.ResourceRequests).HasForeignKey(x => x.RequesterId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.FulfilledByResource).WithMany(x => x.FulfilledRequests).HasForeignKey(x => x.FulfilledByResourceId).OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(t => t.HasCheckConstraint("CK_ResourceRequests_Bounty", "[BountyPoints] IN (20, 40, 60, 80)"));
    }
}

public sealed class ResourceRequestVoteConfiguration : IEntityTypeConfiguration<ResourceRequestVote>
{
    public void Configure(EntityTypeBuilder<ResourceRequestVote> builder)
    {
        builder.HasIndex(x => new { x.ResourceRequestId, x.UserId }).IsUnique();
        builder.HasIndex(x => x.UserId);
        builder.HasOne(x => x.ResourceRequest).WithMany(x => x.Votes).HasForeignKey(x => x.ResourceRequestId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.User).WithMany(x => x.ResourceRequestVotes).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ReputationTransactionConfiguration : IEntityTypeConfiguration<ReputationTransaction>
{
    public void Configure(EntityTypeBuilder<ReputationTransaction> builder)
    {
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(40);
        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IdempotencyKey).HasMaxLength(180).IsRequired();
        builder.Property(x => x.RelatedEntityType).HasMaxLength(80);
        builder.Property(x => x.RelatedEntityId).HasMaxLength(100);
        builder.HasIndex(x => x.IdempotencyKey).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.CreatedAtUtc });
        builder.HasOne(x => x.User).WithMany(x => x.ReputationTransactions).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(t => t.HasCheckConstraint("CK_ReputationTransactions_BalanceAfter", "[BalanceAfter] >= 0"));
    }
}
