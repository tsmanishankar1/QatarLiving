using QLN.Backend.API.Service.V2ContentService;
using QLN.Common.Infrastructure.IService.IContentService;
using QLN.Common.Infrastructure.IService.V2IContent;

namespace QLN.Backend.API.ServiceConfiguration
{
    public static  class ReportsDependencyInjection
    {
        public static IServiceCollection ReportsConfiguration(this IServiceCollection services, IConfiguration config)
        {
            services.AddTransient<IV2ReportsService, V2ExternalReportsService>();
            return services;
        }
    }
}
