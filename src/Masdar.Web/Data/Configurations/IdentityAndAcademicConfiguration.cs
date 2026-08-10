using Masdar.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Masdar.Web.Data.Configurations;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(x => x.DisplayName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.ProfileImageKey).HasMaxLength(300);
        builder.Property(x => x.Bio).HasMaxLength(1000);
        builder.HasIndex(x => new { x.UniversityId, x.MajorId });
        builder.HasOne(x => x.University).WithMany(x => x.Users).HasForeignKey(x => x.UniversityId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Major).WithMany(x => x.Users).HasForeignKey(x => x.MajorId).OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(t => t.HasCheckConstraint("CK_AspNetUsers_ReputationBalance", "[ReputationBalance] >= 0"));
    }
}

public sealed class UniversityConfiguration : IEntityTypeConfiguration<University>
{
    public void Configure(EntityTypeBuilder<University> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(180).IsRequired();
        builder.Property(x => x.NormalizedName).HasMaxLength(180).IsRequired();
        builder.Property(x => x.Location).HasMaxLength(160);
        builder.Property(x => x.WebsiteUrl).HasMaxLength(500);
        builder.Property(x => x.LogoStorageKey).HasMaxLength(300);
        builder.HasIndex(x => x.NormalizedName).IsUnique();
        builder.HasIndex(x => x.IsActive);
    }
}

public sealed class MajorConfiguration : IEntityTypeConfiguration<Major>
{
    public void Configure(EntityTypeBuilder<Major> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(160).IsRequired();
        builder.Property(x => x.NormalizedName).HasMaxLength(160).IsRequired();
        builder.HasIndex(x => new { x.UniversityId, x.NormalizedName }).IsUnique();
        builder.HasIndex(x => new { x.UniversityId, x.IsActive });
        builder.HasOne(x => x.University).WithMany(x => x.Majors).HasForeignKey(x => x.UniversityId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.Property(x => x.Code).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(180).IsRequired();
        builder.Property(x => x.Instructor).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Semester).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.HasIndex(x => new { x.MajorId, x.Code, x.Semester }).IsUnique();
        builder.HasIndex(x => new { x.IsActive, x.Name });
        builder.HasOne(x => x.Major).WithMany(x => x.Courses).HasForeignKey(x => x.MajorId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class UserCourseConfiguration : IEntityTypeConfiguration<UserCourse>
{
    public void Configure(EntityTypeBuilder<UserCourse> builder)
    {
        builder.HasKey(x => new { x.UserId, x.CourseId });
        builder.HasIndex(x => new { x.UserId, x.IsActive });
        builder.HasOne(x => x.User).WithMany(x => x.UserCourses).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Course).WithMany(x => x.UserCourses).HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Cascade);
    }
}
