using QLN.Backend.API.Service.AddonService;
using QLN.Common.Infrastructure.IService.IAddonService;


namespace QLN.Backend.API.ServiceConfiguration
{
    public static class AddonDependencyInjection
    {
        public static IServiceCollection AddonConfiguration(this IServiceCollection services, IConfiguration config)
        {
            services.AddTransient<IAddonService, ExternalAddonService>();
            return services;
        }
    }
}
