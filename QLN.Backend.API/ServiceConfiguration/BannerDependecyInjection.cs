using QLN.Backend.API.Service.V2ContentService;
using QLN.Common.Infrastructure.IService.V2IContent;

namespace QLN.Backend.API.ServiceConfiguration
{
    public static class BannerDependecyInjection
    {
        public static IServiceCollection V2BannerConfiguration(this IServiceCollection services, IConfiguration config)
        {
            services.AddTransient<IV2BannerService, V2ExternalBannerService>();
            return services;
        }
    }
}
