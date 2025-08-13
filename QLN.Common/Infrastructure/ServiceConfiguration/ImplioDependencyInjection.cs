using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using QLN.Common.DTO_s.Implio;
using QLN.Common.DTO_s.Payments;
using QLN.Common.Infrastructure.IService.IImplio;
using QLN.Common.Infrastructure.IService.IPayments;
using QLN.Common.Infrastructure.Service.Implio;
using QLN.Common.Infrastructure.Service.Payments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.ServiceConfiguration
{
    public static class ImplioDependencyInjection
    {
        public static IServiceCollection ImplioConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<ImplioConfig>(configuration.GetSection("Implio"));

            services.AddHttpClient<IImplioService, ImplioService>((serviceProvider, option) =>
            {
                var implioConfig = serviceProvider.GetRequiredService<IOptions<ImplioConfig>>().Value;

                if (string.IsNullOrWhiteSpace(implioConfig.BaseUrl)) throw new ArgumentNullException("BaseUrl");
                if (string.IsNullOrWhiteSpace(implioConfig.ApiKey)) throw new ArgumentNullException("ApiKey");

                if (Uri.TryCreate(implioConfig.BaseUrl, UriKind.Absolute, out var implioUrl))
                {
                    option.BaseAddress = implioUrl;
                    option.DefaultRequestHeaders.Add("X-Api-Key", implioConfig.ApiKey);
                }
                else
                {
                    throw new ArgumentException("Invalid BaseUrl format in ImplioConfig");
                }
            });

            return services;
        }
    }
}
