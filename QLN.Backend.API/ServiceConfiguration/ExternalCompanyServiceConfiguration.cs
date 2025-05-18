using QLN.Backend.API.Service.CompanyService;
using QLN.Common.Infrastructure.IService.ICompanyService;
using System.Windows.Input;

namespace QLN.Backend.API.ServiceConfiguration
{
    public static class ExternalCompanyServiceConfiguration
    {
        public static IServiceCollection ExternalCompanyProfileServiceConfiguration(this IServiceCollection services, IConfiguration config)
        {
            services.AddTransient<ICompanyService, ExternalCompanyService>();
            return services;
        }
    }
}
