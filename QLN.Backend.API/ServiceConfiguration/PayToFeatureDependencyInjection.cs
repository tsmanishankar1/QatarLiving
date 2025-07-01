
using QLN.Backend.API.Service.PayToFeatureService;
using QLN.Common.Infrastructure.IService.IPayToFeatureService;


namespace QLN.Backend.API.ServiceConfiguration
{
    public static class PayToFeatureDepenedencyInjection
    {
        public static IServiceCollection PayToFeatureConfiguration(this IServiceCollection services, IConfiguration config)
        {
            services.AddTransient<IPayToFeatureService, ExternalPayToFeatureService>();
            return services;
        }
    }
}
