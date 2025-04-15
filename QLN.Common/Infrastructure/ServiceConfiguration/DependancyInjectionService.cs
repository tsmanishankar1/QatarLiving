using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            return services;
        }
    }
}
