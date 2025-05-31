using QLN.Backend.API.Service.BannerService;
using QLN.Backend.API.Service.ClassifiedService;
using QLN.Backend.API.Service.ContentService;
using QLN.Common.Infrastructure.IService;
using QLN.Common.Infrastructure.IService.IBannerService;
using QLN.Common.Infrastructure.IService.IContentService;

namespace QLN.Backend.API.ServiceConfiguration
{
    public static class DependencyInjectionService
    {
        public static IServiceCollection ClassifiedServicesConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddTransient<IClassifiedService, ExternalClassifiedService>();

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

    }
}
