using QLN.Backend.API.Service.PayToPublishService;
using QLN.Common.Infrastructure.IService.IPayToPublishService;

namespace QLN.Backend.API.ServiceConfiguration
{
    public static class PayToPublishDepenedencyInjection
    {
        public static IServiceCollection PayToPublishConfiguration(this IServiceCollection services, IConfiguration config)
        {
            services.AddTransient<IPayToPublishService, ExternalPayToPublishService>();
            return services;
        }
    }
}
