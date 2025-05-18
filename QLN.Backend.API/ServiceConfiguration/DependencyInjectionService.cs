using QLN.Backend.API.Service.BannerService;
using QLN.Backend.API.Service.ClassifiedService;
using QLN.Common.Infrastructure.IService;
using QLN.Common.Infrastructure.IService.BannerService;
using QLN.Common.Infrastructure.Service.SaveSearch;

namespace QLN.Backend.API.ServiceConfiguration
{
    public static class DependencyInjectionService
    {
        public static IServiceCollection ClassifiedServicesConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddTransient<IBannerService, ExternalBannerService>();
            services.AddTransient<ISaveSearchService, SaveSearchService>();
            services.AddTransient<IClassifiedService, ExternalClassifiedService>();

            return services;
        }


    }
}
