using Masdar.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Masdar.Web.Data.Configurations;

public sealed class StudyResourceConfiguration : IEntityTypeConfiguration<StudyResource>
{
    public void Configure(EntityTypeBuilder<StudyResource> builder)
    {
        builder.Property(x => x.Title).HasMaxLength(220).IsRequired();
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(40);
        builder.Property(x => x.Semester).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(6000).IsRequired();
        builder.Property(x => x.TutorContext).HasMaxLength(6000);
        builder.Property(x => x.PublicationStatus).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.VerificationStatus).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.FileScanStatus).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.StorageKey).HasMaxLength(300).IsRequired();
        builder.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Sha256).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.Property(x => x.ExternalUrl).HasMaxLength(1000);
        builder.HasIndex(x => new { x.PublicationStatus, x.CourseId, x.Type, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.CourseId, x.VerificationStatus, x.IsRepresentative });
        builder.HasIndex(x => x.Sha256);
        builder.HasOne(x => x.Course).WithMany(x => x.Resources).HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Uploader).WithMany(x => x.UploadedResources).HasForeignKey(x => x.UploaderId).OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(t => t.HasCheckConstraint("CK_StudyResources_FileSize", "[FileSizeBytes] > 0 AND [FileSizeBytes] <= 10485760"));
    }
}

public sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(60).IsRequired();
        builder.Property(x => x.NormalizedName).HasMaxLength(60).IsRequired();
        builder.HasIndex(x => x.NormalizedName).IsUnique();
    }
}

public sealed class ResourceTagConfiguration : IEntityTypeConfiguration<ResourceTag>
{
    public void Configure(EntityTypeBuilder<ResourceTag> builder)
    {
        builder.HasKey(x => new { x.ResourceId, x.TagId });
        builder.HasOne(x => x.Resource).WithMany(x => x.ResourceTags).HasForeignKey(x => x.ResourceId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Tag).WithMany(x => x.ResourceTags).HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ResourceRatingConfiguration : IEntityTypeConfiguration<ResourceRating>
{
    public void Configure(EntityTypeBuilder<ResourceRating> builder)
    {
        builder.HasIndex(x => new { x.ResourceId, x.UserId }).IsUnique();
        builder.HasIndex(x => x.UserId);
        builder.HasOne(x => x.Resource).WithMany(x => x.Ratings).HasForeignKey(x => x.ResourceId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.User).WithMany(x => x.ResourceRatings).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(t => t.HasCheckConstraint("CK_ResourceRatings_Value", "[Value] BETWEEN 1 AND 5"));
    }
}

public sealed class ResourceHelpfulVoteConfiguration : IEntityTypeConfiguration<ResourceHelpfulVote>
{
    public void Configure(EntityTypeBuilder<ResourceHelpfulVote> builder)
    {
        builder.HasIndex(x => new { x.ResourceId, x.UserId }).IsUnique();
        builder.HasIndex(x => x.UserId);
        builder.HasOne(x => x.Resource).WithMany(x => x.HelpfulVotes).HasForeignKey(x => x.ResourceId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.User).WithMany(x => x.HelpfulVotes).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class SavedResourceConfiguration : IEntityTypeConfiguration<SavedResource>
{
    public void Configure(EntityTypeBuilder<SavedResource> builder)
    {
        builder.HasKey(x => new { x.UserId, x.ResourceId });
        builder.HasIndex(x => new { x.UserId, x.SavedAtUtc });
        builder.HasOne(x => x.User).WithMany(x => x.SavedResources).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Resource).WithMany(x => x.SavedByUsers).HasForeignKey(x => x.ResourceId).OnDelete(DeleteBehavior.Cascade);
    }
}
