using Azure.AI.OpenAI;
using QLN.AIPOV.Backend.Application.Interfaces;
using QLN.AIPOV.Backend.Application.Models.Config;
using QLN.AIPOV.Backend.Domain.HttpClients;
using QLN.AIPOV.Backend.Domain.Services;
using System.ClientModel;

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
            services.AddHttpClient();
            services.AddScoped<IChatGPTClient, ChatGPTClient>();
            return services;
        }

        public static IServiceCollection AddAzureOpenAIClient(this IServiceCollection services, IConfiguration configuration)
        {
            // Register your Azure OpenAI client here
            var config = configuration.GetSection("OpenAI").Get<OpenAISettingsModel>();
            if (config == null || string.IsNullOrEmpty(config.ApiKey) || string.IsNullOrEmpty(config.Endpoint))
                throw new ArgumentException("OpenAI API Key and Endpoint must be provided.");

            services.AddSingleton(new AzureOpenAIClient(new Uri(config.Endpoint), new ApiKeyCredential(config.ApiKey)));

            return services;
        }
    }
}
