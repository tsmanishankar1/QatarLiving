using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using QLN.AIPOV.Frontend.ChatBot.Services.Interfaces;

namespace QLN.AIPOV.Frontend.ChatBot.Services.Implementation;

public class AzureSearchService : IAzureSearchService
{
    private readonly SearchClient _searchClient;
    private readonly ILogger<AzureSearchService> _logger;
    private readonly IConfiguration _configuration;

    public AzureSearchService(IConfiguration configuration, ILogger<AzureSearchService> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        var endpoint = configuration["AzureAISearch:Endpoint"];
        var apiKey = configuration["AzureAISearch:ApiKey"];
        var indexName = configuration["AzureAISearch:IndexName"];

        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(indexName))
        {
            throw new InvalidOperationException("Azure Search configuration is missing or incomplete.");
        }

        _searchClient = new SearchClient(
            new Uri(endpoint),
            indexName,
            new AzureKeyCredential(apiKey));
        _logger = logger;
    }

    public async Task<IEnumerable<SearchDocument>> SearchDocumentsAsync(string query, int top = 10)
    {
        try
        {
            var options = new SearchOptions
            {
                IncludeTotalCount = true,
                Size = top,
                Select = { "*" }
            };

            var response = await _searchClient.SearchAsync<SearchDocument>(query, options);
            return response.Value.GetResults().Select(r => r.Document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing standard search with query: {Query}", query);
            throw;
        }
    }

    public async Task<IEnumerable<SearchDocument>> SearchDocumentsHybridAsync(string query, int top = 10)
    {
        try
        {
            // First try with semantic search
            try
            {
                var semanticOptions = new SearchOptions
                {
                    IncludeTotalCount = true,
                    Size = top,
                    QueryType = SearchQueryType.Semantic,
                    SemanticSearch = new SemanticSearchOptions()
                    {
                        SemanticConfigurationName = "default" // Make sure this matches exactly what you created in Azure Portal
                    },
                    Select = { "*" }
                };

                _logger.LogInformation("Attempting semantic search with configuration: 'default'");
                var semanticResponse = await _searchClient.SearchAsync<SearchDocument>(query, semanticOptions);
                _logger.LogInformation("Semantic search successful");
                return semanticResponse.Value.GetResults().Select(r => r.Document);
            }
            catch (RequestFailedException ex) when (ex.Status == 400)
            {
                // Log detailed diagnostic information
                _logger.LogWarning("Semantic search failed with status 400: {ErrorMessage}", ex.Message);

                if (ex.Message.Contains("semanticConfiguration"))
                {
                    _logger.LogWarning("Falling back to standard search. Likely causes of semantic search failure:");
                    _logger.LogWarning("1. Semantic configuration name mismatch - check the exact name in Azure Portal");
                    _logger.LogWarning("2. Azure AI Search tier doesn't support semantic search (requires Basic or higher)");
                    _logger.LogWarning("3. The semantic configuration may need time to propagate after creation");

                    // Fall back to standard search
                    _logger.LogInformation("Executing fallback standard search");
                    return await SearchDocumentsAsync(query, top);
                }

                throw; // Rethrow if it's a different 400 error
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing hybrid search with query: {Query}", query);
            throw;
        }
    }

    public async Task<string> GetIndexDefinitionAsync()
    {
        try
        {
            var endpoint = _configuration["AzureAISearch:Endpoint"];
            var apiKey = _configuration["AzureAISearch:ApiKey"];
            var indexName = _configuration["AzureAISearch:IndexName"];

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(indexName))
                throw new InvalidOperationException("Azure Search configuration is missing or incomplete.");

            // Create a SearchIndexClient to access index metadata
            var searchIndexClient = new SearchIndexClient(new Uri(endpoint), new AzureKeyCredential(apiKey));

            // Get the index definition
            var indexResponse = await searchIndexClient.GetIndexAsync(indexName);
            var indexDefinition = indexResponse.Value;

            // Return field names and their searchable status
            var fieldInfo = indexDefinition.Fields
                .Select(f => $"{f.Name} (Searchable: {f.IsSearchable})")
                .ToList();

            return $"Index '{indexDefinition.Name}' fields: {string.Join(", ", fieldInfo)}";
        }
        catch (Exception ex)
        {
            return $"Error getting index definition: {ex.Message}";
        }
    }
}