using Azure;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
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
            services.Configure<DocumentIntelligenceSettingsModel>(configuration.GetSection("DocumentIntelligence"));
            return services;
        }

        public static IServiceCollection AddApiServices(this IServiceCollection services)
        {
            services.AddScoped<IChatService, ChatService>();
            services.AddScoped<IDocumentService, DocumentService>();
            return services;
        }

        public static IServiceCollection AddHttpClients(this IServiceCollection services)
        {
            services.AddHttpClient();
            services.AddScoped<IChatGPTClient, ChatGPTClient>();
            services.AddScoped<IDocumentIntelligenceClient, DocumentIntelligenceClient>();
            return services;
        }

        public static IServiceCollection AddAzureOpenAIClient(this IServiceCollection services, IConfiguration configuration)
        {
            // Register your Azure OpenAI client here
            var config = configuration.GetSection("OpenAI").Get<OpenAISettingsModel>();
            if (config == null || string.IsNullOrEmpty(config.ApiKey) || string.IsNullOrEmpty(config.Endpoint))
                throw new ArgumentException("OpenAI API Key and Endpoint must be provided.");

            if (string.IsNullOrEmpty(config.Endpoint) || string.IsNullOrEmpty(config.ApiKey))
                throw new ArgumentException("Azure OpenAI configuration is missing.");

            services.AddSingleton(new AzureOpenAIClient(new Uri(config.Endpoint),
                new AzureKeyCredential(config.ApiKey)));

            return services;
        }

        public static IServiceCollection AddAzureAISearchClient(this IServiceCollection services, IConfiguration configuration)
        {
            // Register your Azure AI Search client here
            var searchConfig = configuration.GetSection("AzureAISearch").Get<AzureAISearchSettingsModel>();
            if (searchConfig == null || string.IsNullOrEmpty(searchConfig.SearchApiKey) || string.IsNullOrEmpty(searchConfig.Endpoint))
                throw new ArgumentException("Azure AI Search API Key and Endpoint must be provided.");
            if (string.IsNullOrEmpty(searchConfig.Endpoint) || string.IsNullOrEmpty(searchConfig.SearchApiKey))
                throw new ArgumentException("Azure AI Search configuration is missing.");
            var endpoint = new Uri(searchConfig.Endpoint);
            var credential = new AzureKeyCredential(searchConfig.SearchApiKey);
            services.AddSingleton(new SearchClient(endpoint, searchConfig.SearchIndexName, credential));

            return services;
        }
    }
}
