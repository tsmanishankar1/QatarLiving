using QLN.Classified.MS.Service;
using QLN.Classified.MS.Service.Services;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService;
using QLN.Common.Infrastructure.IService.V2IClassifiedBoService;
using QLN.Classified.MS.Service.ClassifiedBoService;
using QLN.Common.Infrastructure.IService.IService;
using QLN.Common.Infrastructure.IService.IServiceBoService;
using QLN.Classified.MS.Service.ServicesBoService;
using QLN.Common.Infrastructure.IService.ISubscriptionService;
using QLN.Common.Infrastructure.IService.IClassifiedBoService;
using QLN.Common.Infrastructure.IService.ISearchService;


namespace QLN.Classifieds.MS.ServiceConfiguration
{
    public static class DependencyInjectionService
    {
        public static IServiceCollection ClassifiedInternalServicesConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IClassifiedBoLandingService, InternalClassifiedLandigBo>();
            services.AddTransient<IClassifiedStoresBOService, InternalClassifiedStoresBOService>();
            services.AddScoped<IClassifiedPreLovedBOService, InternalClassifiedPreLovedBOService>();
            services.AddTransient<IClassifiedService, ClassifiedService>();
            services.AddTransient<IClassifiedsFoService, ClassifiedFoService>();
            services.AddTransient<IServices, InternalServicesService>();
            services.AddTransient<IServicesBoService, InternalServicesBo>();
 
            return services;
        }
    }
}
