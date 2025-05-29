using QLN.SearchService.IRepository;
using QLN.SearchService.IService;
using QLN.SearchService.Repository;
using QLN.SearchService.Service;

namespace QLN.SearchService.ServiceConfiguration
{
    public static class DependencyInjectionService
    {
        public static IServiceCollection SearchConfiguration(this IServiceCollection services, IConfiguration config)
        {
           services.AddScoped<ISearchRepository, SearchRepository>();
           services.AddScoped<ISearchService, SearchServices>();
           services.AddSingleton<ISearchIndexInitializer, SearchIndexInitializer>();
           services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();
           services.AddScoped<IAnalyticsService, AnalyticsService>();
            return services;
        }
    }
}
