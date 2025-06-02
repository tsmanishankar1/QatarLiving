using QLN.Classified.MS.Service;
using QLN.Common.Infrastructure.IService;

namespace QLN.Classifieds.MS.ServiceConfiguration
{
    public static class DependencyInjectionService
    {
        public static IServiceCollection ClassifiedInternalServicesConfiguration(this IServiceCollection services, IConfiguration configuration)
        {

            services.AddTransient<IClassifiedService, ClassifiedService>();
            return services;
        }
    }
}
