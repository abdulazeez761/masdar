namespace Masdar.Web.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMasdarServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FileStorageOptions>(configuration.GetSection(FileStorageOptions.SectionName));
        services.Configure<GeminiRagOptions>(configuration.GetSection(GeminiRagOptions.SectionName));
        services.AddMemoryCache();
        services.AddHttpClient<GeminiApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/v1beta/");
            client.Timeout = TimeSpan.FromMinutes(2);
        });
        services.AddHttpContextAccessor();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IResourceService, ResourceService>();
        services.AddScoped<ICommunityService, CommunityService>();
        services.AddScoped<IOpportunityService, OpportunityService>();
        services.AddScoped<ICourseService, CourseService>();
        services.AddScoped<IRagDocumentTextExtractor, RagDocumentTextExtractor>();
        services.AddScoped<IGroundedRagService, GeminiGroundedRagService>();
        services.AddScoped<ITutorService, CourseTutorService>();
        return services;
    }
}
