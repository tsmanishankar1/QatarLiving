using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using QLN.Common.Indexing.IndexModels;
using QLN.Common.Indexing.IService;
using QLN.Common.Indexing.Service;
using QLN.Common.Infrastructure.RepositoryConfiguration;
using QLN.Common.Infrastructure.Service;
using QLN.Common.Infrastructure.ServiceInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.ServiceConfiguration
{
    public static class DependancyInjectionServices
    {
        public static IServiceCollection ServicesConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IAuthService, AuthService>();
            services.RepositoryConfiguration(configuration);
            services.Configure<AzureSearchOptions>(options =>
                configuration.GetSection("AzureSearch").Bind(options));

            services.AddSingleton<ISearchService<UserIndex>>(sp =>
            {
                var azureOptions = sp.GetRequiredService<IOptions<AzureSearchOptions>>().Value;
                return new SearchService<UserIndex>(
                    azureOptions.ServiceEndpoint,
                    azureOptions.AdminApiKey);
            });
            return services;
        }
    }
}
