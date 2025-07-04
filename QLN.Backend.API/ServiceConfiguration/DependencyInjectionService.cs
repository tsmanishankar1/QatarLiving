using QLN.Backend.API.Service.AnalyticsService;
using QLN.Backend.API.Service.BannerService;
using QLN.Backend.API.Service.ClassifiedService;
using QLN.Backend.API.Service.CompanyService;
using QLN.Backend.API.Service.ContentService;
using QLN.Backend.API.Service.SearchService;
using QLN.Common.Infrastructure.IService;
using QLN.Common.Infrastructure.IService.ICompanyService;
using QLN.Common.Infrastructure.IService.IBannerService;
using QLN.Common.Infrastructure.IService.IContentService;
using QLN.Common.Infrastructure.IService.ISearchService;
using QLN.Backend.API.Service.ServicesService;
using QLN.Common.Infrastructure.IService.IFileStorage;
using QLN.Common.Infrastructure.Service.FileStorage;
using QLN.Common.Infrastructure.IService.IBackOfficeService;
using QLN.Backend.API.Service.BackOffice;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.V2IContent;
using QLN.Backend.API.Service.V2ContentService;

namespace QLN.Backend.API.ServiceConfiguration
{
    public static class DependencyInjectionService
    {
        public static IServiceCollection ClassifiedServicesConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddTransient<IClassifiedService, ExternalClassifiedService>();
            services.AddTransient<IServicesService, ExternalServiceService>();
            services.AddScoped<IFileStorageBlobService, FileStorageBlobService>();
            services.AddTransient<IBackOfficeService<LandingBackOfficeIndex>, ExternalLandingBackOfficeService>();

            return services;
        }
        public static IServiceCollection AnalyticsServicesConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddTransient<IAnalyticsService, ExternalAnalyticsService>();

            return services;
        }
        public static IServiceCollection SearchServicesConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddTransient<ISearchService, ExternalSearchService>();

            return services;
        }
        public static IServiceCollection ContentServicesConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            var drupalUrl = configuration.GetSection("BaseUrl")["LegacyDrupal"] ?? throw new ArgumentNullException("LegacyDrupal");

            if (Uri.TryCreate(drupalUrl, UriKind.Absolute, out var drupalBaseUrl))
            {
                services.AddHttpClient<IContentService, ExternalContentService>(option => 
                    {
                        option.BaseAddress = drupalBaseUrl;
                    });
            }

            return services;
        }

        public static IServiceCollection BannerServicesConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            var drupalUrl = configuration.GetSection("BaseUrl")["LegacyDrupal"] ?? throw new ArgumentNullException("LegacyDrupal");

            if (Uri.TryCreate(drupalUrl, UriKind.Absolute, out var drupalBaseUrl))
            {
                services.AddHttpClient<IBannerService, ExternalBannerService>(option =>
                {
                    option.BaseAddress = drupalBaseUrl;
                });
            }

            return services;
        }

        public static IServiceCollection CompanyConfiguration(this IServiceCollection services, IConfiguration config)
        {
            services.AddTransient<ICompanyService, ExternalCompanyService>();
            return services;
        }
        public static IServiceCollection EventConfiguration(this IServiceCollection services, IConfiguration config)
        {
            services.AddTransient<IV2EventService, V2ExternalEventService>();
            return services;
        }
      
        public static IServiceCollection NewsConfiguration(this IServiceCollection services, IConfiguration config)
        {
            services.AddTransient<IV2NewsService, V2ExternalNewsService>();
            return services;
        }
        public static IServiceCollection CommunityConfiguration(this IServiceCollection services, IConfiguration config)
        {
            services.AddTransient<V2IContentLocation, V2ExternalLocationService>();
            return services;
        }
        public static IServiceCollection CommunityPostConfiguration(this IServiceCollection services, IConfiguration config)
        {
            services.AddTransient<IV2CommunityPostService, V2ExternalCommunityPostService>();
            return services;
        }

    }
}
