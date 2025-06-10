using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Services;

namespace QLN.Web.Shared
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddWebSharedServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddMudServices();
            services.AddScoped<GlobalAppState>();
             services.AddScoped<SearchStateService>();
            var section = configuration.GetSection("ApiSettings");
            services.Configure<ApiSettings>(configuration.GetSection("ApiSettings"));
            services.AddHttpClient<ApiService>();



            return services;
        }
    }
}
