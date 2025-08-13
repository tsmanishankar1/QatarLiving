using QLN.Backend.API.Service.ProductService;
using QLN.Backend.API.Service.SubscriptionService;
using QLN.Common.Infrastructure.IService.IProductService;
using QLN.Common.Infrastructure.IService.ISubscriptionService;

namespace QLN.Backend.API.ServiceConfiguration
{
    public static class SubscriptionDependencyInjection
    {
        public static IServiceCollection SubscriptionConfiguration(this IServiceCollection services, IConfiguration config)
        {
            services.AddTransient<IExternalSubscriptionService, ExternalSubscriptionService>();
            services.AddScoped<IUserQuotaService, UserQuotaService>();
            services.AddScoped<IV2SubscriptionService, V2SubscriptionService>();
            return services;
        }
    }
}
