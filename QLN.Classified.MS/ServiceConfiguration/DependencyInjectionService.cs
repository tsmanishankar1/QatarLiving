using QLN.Classified.MS.Service;
using QLN.Common.Infrastructure.IService;

namespace QLN.Common.Infrastructure.ServiceConfiguration
{
    public static class DependencyInjectionService
    {
        public static IServiceCollection ClassifiedServicesConfiguration(this IServiceCollection services, IConfiguration configuration)
        {

            services.AddTransient<IClassifiedService, ClassifiedService>();
            return services;
        }
    }
}
