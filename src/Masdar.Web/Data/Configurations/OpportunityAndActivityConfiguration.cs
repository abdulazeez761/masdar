using Masdar.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Masdar.Web.Data.Configurations;

public sealed class OpportunityConfiguration : IEntityTypeConfiguration<Opportunity>
{
    public void Configure(EntityTypeBuilder<Opportunity> builder)
    {
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(40);
        builder.Property(x => x.Title).HasMaxLength(220).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(3000).IsRequired();
        builder.Property(x => x.Location).HasMaxLength(180);
        builder.Property(x => x.RegistrationUrl).HasMaxLength(1000);
        builder.HasIndex(x => new { x.IsPublished, x.IsActive, x.StartsAtUtc });
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Opportunities_Capacity", "[Capacity] IS NULL OR [Capacity] > 0");
            t.HasCheckConstraint("CK_Opportunities_Dates", "[EndsAtUtc] IS NULL OR [EndsAtUtc] >= [StartsAtUtc]");
        });
    }
}

public sealed class OpportunityRegistrationConfiguration : IEntityTypeConfiguration<OpportunityRegistration>
{
    public void Configure(EntityTypeBuilder<OpportunityRegistration> builder)
    {
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        builder.HasIndex(x => new { x.OpportunityId, x.UserId }).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.Status });
        builder.HasOne(x => x.Opportunity).WithMany(x => x.Registrations).HasForeignKey(x => x.OpportunityId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.User).WithMany(x => x.OpportunityRegistrations).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.Property(x => x.Action).HasMaxLength(100).IsRequired();
        builder.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.EntityId).HasMaxLength(100);
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.MetadataJson).HasMaxLength(8000);
        builder.Property(x => x.IpAddress).HasMaxLength(64);
        builder.Property(x => x.UserAgent).HasMaxLength(512);
        builder.Property(x => x.CorrelationId).HasMaxLength(100);
        builder.HasIndex(x => new { x.EntityType, x.EntityId, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.ActorUserId, x.CreatedAtUtc });
        builder.HasOne(x => x.ActorUser).WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.SetNull);
    }
}
