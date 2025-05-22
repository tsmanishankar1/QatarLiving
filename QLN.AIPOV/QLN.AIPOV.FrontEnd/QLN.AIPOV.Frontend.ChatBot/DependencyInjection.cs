using QLN.AIPOV.FrontEnd.ChatBot.Services.Implementation;
using QLN.AIPOV.FrontEnd.ChatBot.Services.Interfaces;

namespace QLN.AIPOV.FrontEnd.ChatBot
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
