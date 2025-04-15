using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QLN.Common.Infrastructure.Repository;
using QLN.Common.Infrastructure.RepositoryInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.RepositoryConfiguration
{
    public static class DependancyInjectionRepo
    {
        public static IServiceCollection RepositoryConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IAuthRepository, AuthRepository>();
           services.AddScoped<IEventlogger,Eventlogger>();




            return services;
        }
    }
}
