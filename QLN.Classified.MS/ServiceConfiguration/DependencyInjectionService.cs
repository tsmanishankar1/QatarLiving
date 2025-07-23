using QLN.Classified.MS.Service;
using QLN.Classified.MS.Service.BackOfficeService;
using QLN.Classified.MS.Service.Services;
using QLN.Classified.MS.Service.ServicesAdService;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService;
using QLN.Common.Infrastructure.IService.IBackOfficeService;
using QLN.Common.Infrastructure.IService.V2IClassifiedBoService;
using QLN.Content.MS.Service.ClassifiedBoService;
using QLN.Common.Infrastructure.IService.IService;


namespace QLN.Classifieds.MS.ServiceConfiguration
{
    public static class DependencyInjectionService
    {
        public static IServiceCollection ClassifiedInternalServicesConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IClassifiedBoLandingService, InternalClassifiedLandigBo>();

            services.AddTransient<IClassifiedService, ClassifiedService>();
            services.AddTransient<IServicesService, ServicesAdService>();
            services.AddTransient<IServices, InternalServicesService>();
            services.AddTransient<IBackOfficeService<LandingBackOfficeIndex>, InternalLandingBackOfficeService>();
            return services;
        }
    }
}
