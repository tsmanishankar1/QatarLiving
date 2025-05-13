using QLN.Backend.API.Service.BannerService;
using QLN.Common.Infrastructure.IService.BannerService;

namespace QLN.Backend.API.ServiceConfiguration
{
    public static class DependencyInjectionService
    {
        public static IServiceCollection ClassifiedServicesConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddTransient<IBannerService, ExternalBannerService>();

            return services;
        }
    }
}
