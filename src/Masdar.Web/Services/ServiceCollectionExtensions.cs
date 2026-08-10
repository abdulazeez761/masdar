namespace Masdar.Web.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMasdarServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FileStorageOptions>(configuration.GetSection(FileStorageOptions.SectionName));
        services.AddHttpContextAccessor();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IResourceService, ResourceService>();
        services.AddScoped<ICommunityService, CommunityService>();
        services.AddScoped<IOpportunityService, OpportunityService>();
        services.AddScoped<ICourseService, CourseService>();
        services.AddScoped<ITutorService, CourseTutorService>();
        return services;
    }
}
