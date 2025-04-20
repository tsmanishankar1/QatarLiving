using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QLN.Common.IService;
using QLN.Common.Model;
using QLN.Common.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.ServiceConfiguration
{
    public static class DependencyInjectionService
    {
        public static IServiceCollection ServicesConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddTransient<IEmailSender<ApplicationUser>, SmtpEmailSender>();
            services.AddScoped<ITokenService, TokenService>();
            return services;
        }
    }
}
