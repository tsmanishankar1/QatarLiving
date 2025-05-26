using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QLN.Common.Infrastructure.EventLogger;
using QLN.Common.Infrastructure.IService.IAuthService;
using QLN.Common.Infrastructure.IService.IEmailService;
using QLN.Common.Infrastructure.IService.IFileStorage;
using QLN.Common.Infrastructure.IService.ITokenService;
using QLN.Common.Infrastructure.Model;
using QLN.Common.Infrastructure.Service.AuthService;
using QLN.Common.Infrastructure.Service.FileStorage;
using QLN.Common.Infrastructure.Service.JwtTokenService;
using QLN.Common.Infrastructure.Service.SmtpService;

namespace QLN.Common.Infrastructure.ServiceConfiguration
{
    public static class DependencyInjectionService
    {
        public static IServiceCollection ServicesConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddTransient<IEmailSender<ApplicationUser>, EmailSenderService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IExtendedEmailSender<ApplicationUser>, EmailSenderService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddTransient<QLN.Common.Infrastructure.TokenProvider.EmailTokenProvider<ApplicationUser>>();
            services.AddTransient<QLN.Common.Infrastructure.TokenProvider.CommonTokenProvider<ApplicationUser>>();            
            services.AddScoped<IEventlogger, Eventlogger>();
            services.AddScoped<IFileStorageService, FileStorageService>();

            return services;
        }
    }
}
