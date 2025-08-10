using QLN.Backend.API.Service.AnalyticsService;
using QLN.Backend.API.Service.BannerService;
using QLN.Backend.API.Service.ClassifiedService;
using QLN.Backend.API.Service.CompanyService;
using QLN.Backend.API.Service.ContentService;
using QLN.Backend.API.Service.DrupalAuthService;
using QLN.Backend.API.Service.SearchService;
using QLN.Backend.API.Service.ServiceBoService;
using QLN.Backend.API.Service.Services;
using QLN.Backend.API.Service.V2ClassifiedBoService;
using QLN.Backend.API.Service.V2ContentService;
using QLN.Common.Infrastructure.IService;
using QLN.Common.Infrastructure.IService.IAuth;
using QLN.Common.Infrastructure.IService.IBannerService;
using QLN.Common.Infrastructure.IService.ICompanyService;
using QLN.Common.Infrastructure.IService.IContentService;
using QLN.Common.Infrastructure.IService.IFileStorage;
using QLN.Common.Infrastructure.IService.ISearchService;
using QLN.Common.Infrastructure.IService.V2IClassifiedBoService;
using QLN.Common.Infrastructure.IService.IService;
using QLN.Common.Infrastructure.IService.V2IContent;
using QLN.Common.Infrastructure.Service.FileStorage;
using QLN.Common.Infrastructure.IService.IServiceBoService;
using QLN.Common.Infrastructure.IService.IClassifiedBoService;
using QLN.Backend.API.Service.ClassifiedBoService;

namespace QLN.Backend.API.ServiceConfiguration
{
    public static class DependencyInjectionService
    {
        public static IServiceCollection FileServiceConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IFileStorageBlobService, FileStorageBlobService>(); // need to inject in the Conenction string here rather

            return services;
        }

        public static IServiceCollection ClassifiedServicesConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddTransient<IClassifiedService, ExternalClassifiedService>();
            services.AddScoped<IFileStorageBlobService, FileStorageBlobService>();

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

        public static IServiceCollection DrupalAuthConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            var drupalUrl = configuration.GetSection("BaseUrl")["LegacyDrupal"] ?? throw new ArgumentNullException("LegacyDrupal");

            if (Uri.TryCreate(drupalUrl, UriKind.Absolute, out var drupalBaseUrl))
            {
                services.AddHttpClient<IDrupalAuthService, DrupalAuthService>(option =>
                {
                    option.BaseAddress = drupalBaseUrl;
                });
            }

            return services;
        }
        public static IServiceCollection DrupalUserServicesConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            var drupalUrl = configuration.GetSection("BaseUrl")["LegacyDrupalUser"] ?? throw new ArgumentNullException("LegacyDrupalUser");

            if (Uri.TryCreate(drupalUrl, UriKind.Absolute, out var drupalBaseUrl))
            {
                services.AddHttpClient<IDrupalUserService, ExternalDrupalUserService>(option =>
                {
                    option.BaseAddress = drupalBaseUrl;
                });
            }

            return services;
        }

        public static IServiceCollection CompanyConfiguration(this IServiceCollection services, IConfiguration config)
        {
            services.AddTransient<ICompanyProfileService, ExternalCompanyProfileService>();
            return services;
        }
        public static IServiceCollection EventConfiguration(this IServiceCollection services, IConfiguration config)
        {
            services.AddTransient<IV2EventService, V2ExternalEventService>();
            return services;
        }
        public static IServiceCollection EventFOConfiguration(this IServiceCollection services, IConfiguration config)
        {
            services.AddTransient<IV2FOEventService, V2FOExternalEventService>();
            return services;
        }
        public static IServiceCollection ServiceConfiguration(this IServiceCollection services, IConfiguration config)
        {
            services.AddTransient<IServices, ExternalServicesService>();
            return services;
        }
        public static IServiceCollection NewsConfiguration(this IServiceCollection services, IConfiguration config)
        {
            services.AddTransient<IV2NewsService, V2ExternalNewsService>();
            return services;
        }
        public static IServiceCollection DailyBoConfiguration(this IServiceCollection services, IConfiguration config)
        {
            services.AddTransient<IV2ContentDailyService, V2ExternalDailyService>();
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


        //clasified bo
        public static IServiceCollection ClassifiedLandingBo(this IServiceCollection services, IConfiguration config)
        {
            services.AddTransient<IClassifiedBoLandingService, ExternalClassifiedLandingService>();
            return services;
        }
        public static IServiceCollection ServicesBo(this IServiceCollection services, IConfiguration config)
        {
            services.AddTransient<IServicesBoService,ExternalServicesBoService>();
            return services;
        }
        public static IServiceCollection ClassifiedBoStoresConfiguration(this IServiceCollection services, IConfiguration config)
        {
            services.AddTransient<IClassifiedStoresBOService, ExternalClassifiedStoresBOService>();
            services.AddTransient<IClassifiedPreLovedBOService, ExternalClassifiedPreLovedBOService>();
            return services;
        }
        
    }
}
