using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using QLN.Common.DTO_s.Payments;
using QLN.Common.Infrastructure.IService.IPayments;
using QLN.Common.Infrastructure.Service.Payments;

namespace QLN.Common.Infrastructure.ServiceConfiguration
{
    public static class PaymentsDependencyInjection
    {
        public static IServiceCollection PaymentsConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<FatoraConfig>(configuration.GetSection("Fatora"));

            services.AddHttpClient<IFatoraService, FatoraService>((serviceProvider, option) =>
            {
                var faturaConfig = serviceProvider.GetRequiredService<IOptions<FatoraConfig>>().Value;

                if (string.IsNullOrWhiteSpace(faturaConfig.ApiUrl)) throw new ArgumentNullException("ApiUrl");

                if (Uri.TryCreate(faturaConfig.ApiUrl, UriKind.Absolute, out var fatoraUrl))
                {
                    option.BaseAddress = fatoraUrl;
                }
                else
                {
                    throw new ArgumentException("Invalid ApiUrl format in FatoraConfig");
                }
            });

            services.AddScoped<IPaymentService, PaymentService>();

            return services;
        }
    }
}
