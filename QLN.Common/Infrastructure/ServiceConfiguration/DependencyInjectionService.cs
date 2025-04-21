using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QLN.Common.Infrastructure.IService;
using QLN.Common.Infrastructure.Model;
using QLN.Common.Infrastructure.Service;


namespace QLN.Common.Infrastructure.ServiceConfiguration
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
