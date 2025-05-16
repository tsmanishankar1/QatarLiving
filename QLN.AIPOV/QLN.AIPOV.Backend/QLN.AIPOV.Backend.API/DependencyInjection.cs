using Microsoft.Extensions.Options;
using QLN.AIPOV.Backend.Application.Interfaces;
using QLN.AIPOV.Backend.Application.Models.Config;
using QLN.AIPOV.Backend.Domain.HttpClients;
using QLN.AIPOV.Backend.Domain.Services;

namespace QLN.AIPOV.Backend.API
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApiConfig(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<OpenAISettingsModel>(configuration.GetSection("OpenAI"));
            return services;
        }

        public static IServiceCollection AddApiServices(this IServiceCollection services)
        {
            services.AddScoped<IChatService, ChatService>();
            return services;
        }

        public static IServiceCollection AddHttpClients(this IServiceCollection services)
        {
            // Register your HttpClient here
            services.AddHttpClient<IChatGPTClient, ChatGPTClient>((provider, client) =>
            {
                var openAISettings = provider.GetRequiredService<IOptions<OpenAISettingsModel>>().Value;
                client.BaseAddress = new Uri(openAISettings.Endpoint);
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", openAISettings.ApiKey);
                client.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            });
            return services;
        }
    }
}
