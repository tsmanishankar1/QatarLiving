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

            var searchConfig = configuration.GetSection("AzureAISearch");
            if (searchConfig == null)
                throw new ArgumentException("Azure AI Search configuration is missing.");
            var searchClientEndpoint = searchConfig["Endpoint"];
            if (string.IsNullOrEmpty(searchClientEndpoint) || string.IsNullOrEmpty(searchConfig["SearchApiKey"]) || string.IsNullOrEmpty(searchConfig["SearchIndexName"]))
                throw new ArgumentException("Azure AI Search API Key, Endpoint, and Index Name must be provided.");
            var searchClientApiKey = searchConfig["SearchApiKey"];
            if (string.IsNullOrEmpty(searchClientApiKey))
                throw new ArgumentException("Azure AI Search API Key is missing.");

            services.AddSingleton(new SearchClient(
                new Uri(searchClientEndpoint),
                searchConfig["SearchIndexName"],
                new AzureKeyCredential(searchClientApiKey)));
            return services;
        }

        public static IServiceCollection AddApiServices(this IServiceCollection services)
        {
            services.AddScoped<IChatService, ChatService>();
            services.AddScoped<IDocumentService, DocumentService>();

            services.AddScoped<ISearchService, SearchService>();
            services.AddScoped<IEmbeddingService, AzureOpenAIEmbeddingService>();
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
            var config = configuration.GetSection("OpenAI").Get<OpenAISettingsModel>();
            if (config == null || string.IsNullOrEmpty(config.ApiKey) || string.IsNullOrEmpty(config.Endpoint))
                throw new ArgumentException("OpenAI API Key and Endpoint must be provided.");

            // Register AzureOpenAIClient for both chat and embeddings
            services.AddSingleton(new AzureOpenAIClient(
                new Uri(config.Endpoint),
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

        public static IServiceCollection AddSearchServices(this IServiceCollection services)
        {
            services.AddScoped<IEmbeddingService, AzureOpenAIEmbeddingService>();
            services.AddScoped<ISearchService, SearchService>();
            return services;
        }
    }
}
