using QLN.AIPOV.Frontend.ChatBot.Services.Implementation;
using QLN.AIPOV.Frontend.ChatBot.Services.Interfaces;
using QLN.AIPOV.FrontEnd.ChatBot.Services.Implementation;
using QLN.AIPOV.FrontEnd.ChatBot.Services.Interfaces;

namespace QLN.AIPOV.FrontEnd.ChatBot
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            services.AddScoped<ICvAnalyzerService, CVAnalyzerService>();
            services.AddScoped<IAzureSearchService, AzureSearchService>();
            return services;
        }

        public static IServiceCollection AddHttpClientServices(this IServiceCollection services, IConfiguration configuration)
        {
            var chatBackendUrl = configuration.GetValue<string>("ChatBackend:Endpoint")
                                 ?? throw new InvalidOperationException("ChatBackend:Endpoint is not configured");
            services.AddHttpClient<IChatService, ChatService>(client => { client.BaseAddress = new Uri(chatBackendUrl); });
            services.AddHttpClient<ISearchService, SearchService>(client => { client.BaseAddress = new Uri(chatBackendUrl); });
            var formRecognizerUrl = configuration.GetValue<string>("FormRecognizer:Endpoint")
                                       ?? throw new InvalidOperationException("FormRecognizer:Endpoint is not configured");
            services.AddHttpClient<ICvAnalyzerService, CVAnalyzerService>(client => { client.BaseAddress = new Uri(formRecognizerUrl); });
            return services;
        }
    }
}
