using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QLN.Common.Infrastructure.EventLogger;
using QLN.Common.Infrastructure.IService;
using QLN.Common.Infrastructure.Model;
using QLN.Common.Infrastructure.Service;
using QLN.Common.Infrastructure.TokenProvider;


namespace QLN.Common.Infrastructure.ServiceConfiguration
{
    public static class DependencyInjectionService
    {
        public static IServiceCollection ServicesConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddTransient<IEmailSender<ApplicationUser>, SmtpEmailSender>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IExtendedEmailSender<ApplicationUser>, SmtpEmailSender>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddTransient<QLN.Common.Infrastructure.TokenProvider.EmailTokenProvider<ApplicationUser>>();
            services.AddTransient<QLN.Common.Infrastructure.TokenProvider.PhoneTokenProvider<ApplicationUser>>();
            services.AddTransient<IEventlogger, Eventlogger>();

            return services;
        }
    }
}
