using QLN.Backend.API.Service.BannerService;
using QLN.Backend.API.Service.ClassifiedService;
using QLN.Backend.API.Service.CompanyService;
using QLN.Common.Infrastructure.IService;
using QLN.Common.Infrastructure.IService.BannerService;
using QLN.Common.Infrastructure.IService.ICompanyService;

namespace QLN.Backend.API.ServiceConfiguration
{
    public static class DependencyInjectionService
    {
        public static IServiceCollection ClassifiedServicesConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddTransient<IBannerService, ExternalBannerService>();
            services.AddTransient<IClassifiedService, ExternalClassifiedService>();

            return services;
        }
        public static IServiceCollection CompanyConfiguration(this IServiceCollection services, IConfiguration config)
        {
            services.AddTransient<ICompanyService, ExternalCompanyService>();
            return services;
        }
    }
}
