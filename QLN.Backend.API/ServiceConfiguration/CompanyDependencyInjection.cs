using QLN.Backend.API.Service.CompanyService;
using QLN.Common.Infrastructure.IService.ICompanyService;
using System.Windows.Input;

namespace QLN.Backend.API.ServiceConfiguration
{
    public static class CompanyDependencyInjection
    {
        public static IServiceCollection CompanyConfiguration(this IServiceCollection services, IConfiguration config)
        {
            services.AddTransient<ICompanyService, ExternalCompanyService>();
            return services;
        }
    }
}
