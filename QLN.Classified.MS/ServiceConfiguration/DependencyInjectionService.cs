using QLN.Classified.MS.Service;
using QLN.Classified.MS.Service.BannerService;
using QLN.Common.Infrastructure.IService.BannerService;

namespace QLN.Common.Infrastructure.ServiceConfiguration
{
    public static class DependencyInjectionService
    {
        public static IServiceCollection ClassifiedServicesConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddTransient<IBannerService, BannerService>();

            services.AddTransient<IClassifiedService, ClassifiedService>();
            return services;
        }
    }
}
