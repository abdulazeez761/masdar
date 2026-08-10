using Masdar.Web.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Masdar.Web.Data;

public sealed class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<University> Universities => Set<University>();
    public DbSet<Major> Majors => Set<Major>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<UserCourse> UserCourses => Set<UserCourse>();
    public DbSet<StudyResource> StudyResources => Set<StudyResource>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<ResourceTag> ResourceTags => Set<ResourceTag>();
    public DbSet<ResourceRating> ResourceRatings => Set<ResourceRating>();
    public DbSet<ResourceHelpfulVote> ResourceHelpfulVotes => Set<ResourceHelpfulVote>();
    public DbSet<SavedResource> SavedResources => Set<SavedResource>();
    public DbSet<RoadmapNode> RoadmapNodes => Set<RoadmapNode>();
    public DbSet<UserRoadmapProgress> UserRoadmapProgress => Set<UserRoadmapProgress>();
    public DbSet<ResourceRequest> ResourceRequests => Set<ResourceRequest>();
    public DbSet<ResourceRequestVote> ResourceRequestVotes => Set<ResourceRequestVote>();
    public DbSet<Opportunity> Opportunities => Set<Opportunity>();
    public DbSet<OpportunityRegistration> OpportunityRegistrations => Set<OpportunityRegistration>();
    public DbSet<ReputationTransaction> ReputationTransactions => Set<ReputationTransaction>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
