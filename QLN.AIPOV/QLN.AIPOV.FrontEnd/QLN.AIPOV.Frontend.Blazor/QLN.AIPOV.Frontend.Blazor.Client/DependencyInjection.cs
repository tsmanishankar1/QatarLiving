using QLN.AIPOV.Frontend.Blazor.Client.Services.Implementation;
using QLN.AIPOV.Frontend.Blazor.Client.Services.Interfaces;

namespace QLN.AIPOV.Frontend.Blazor.Client
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            return services;
        }

        public static IServiceCollection AddHttpClientServices(this IServiceCollection services, IConfiguration configuration)
        {
            var chatBackendUrl = configuration.GetValue<string>("ChatBackend:Endpoint")
                                 ?? throw new InvalidOperationException("ChatBackend:Endpoint is not configured");
            services.AddHttpClient<IChatService, ChatService>(client => { client.BaseAddress = new Uri(chatBackendUrl); });
            return services;
        }
    }

}
