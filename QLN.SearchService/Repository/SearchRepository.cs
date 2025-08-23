// File: QLN.SearchService.Repository/SearchRepository.cs
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IRepository.ISearchServiceRepository;
using QLN.SearchService.IndexModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QLN.SearchService.Repository
{
    public class SearchRepository : ISearchRepository
    {
        private readonly AzureSearchSettings _settings;
        private readonly AzureKeyCredential _credential;
        private readonly Uri _endpoint;
        private readonly ILogger<SearchRepository> _logger;

        public SearchRepository(
            IOptions<AzureSearchSettings> opts,
            ILogger<SearchRepository> logger)
        {
            _settings = opts.Value;
            _endpoint = new Uri(_settings.Endpoint);
            _credential = new AzureKeyCredential(_settings.ApiKey);
            _logger = logger;
        }

        private SearchClient GetClient(string IndexName)
        {
            if (!_settings.Indexes.TryGetValue(IndexName, out var indexName))
                throw new ArgumentException($"No index configured for '{IndexName}'", nameof(IndexName));
            return new SearchClient(_endpoint, indexName, _credential);
        }

        public async Task<AzureSearchResults<T>> SearchAsync<T>(
     string indexName,
     SearchOptions? options,
     string? searchText)
        {
            var client = GetClient(indexName);

            try
            {
                _logger.LogInformation(
                    "Entered SearchAsync for Index: '{IndexName}' with SearchText: '{SearchText}'",
                    indexName, searchText);

                
                if (!string.IsNullOrWhiteSpace(searchText) && searchText != "*")
                {
                    _logger.LogInformation("Suggesting '{IndexName}' for '{Text}'", indexName, searchText);

                    
                    var suggestResp = await client.SuggestAsync<SearchDocument>(
                        searchText,
                        suggesterName: "sg",
                        new SuggestOptions
                        {
                            Size = options?.Size ?? 10,
                            UseFuzzyMatching = true,
                            Select = { "Id" }
                        }
                    );

                    var ids = suggestResp.Value.Results
                                .Select(r => r.Document.GetString("Id"))
                                .Where(id => !string.IsNullOrEmpty(id))
                                .ToList();

                    if (ids.Count == 0)
                    {
                        _logger.LogInformation("No suggestions found for '{Text}'", searchText);
                        return new AzureSearchResults<T>
                        {
                            Items = new List<T>(),
                            TotalCount = 0
                        };
                    }

                    
                    var filter = string.Join(" or ", ids.Select(id => $"Id eq '{id}'"));

                    var searchOptions = options ?? new SearchOptions();
                    searchOptions.IncludeTotalCount = true;
                    searchOptions.Size = ids.Count;
                    searchOptions.Filter = filter;

                    _logger.LogInformation("Fetching full documents for suggested IDs: {Ids}", string.Join(",", ids));

                    
                    var resp = await client.SearchAsync<T>("*", searchOptions);

                    var items = new List<T>();
                    await foreach (var r in resp.Value.GetResultsAsync())
                    {
                        items.Add(r.Document!);
                        _logger.LogInformation("Fetched document: {Document}", r.Document);
                    }

                    return new AzureSearchResults<T>
                    {
                        Items = items,
                        TotalCount = resp.Value.TotalCount.GetValueOrDefault()
                    };
                }
                else
                {
                    
                    _logger.LogInformation("SearchText is '*' or empty. Using SearchAsync to fetch all data.");

                    var searchOptions = options ?? new SearchOptions();
                    searchOptions.IncludeTotalCount = true;
                    searchOptions.Size = options?.Size ?? 100;

                    if (!string.IsNullOrWhiteSpace(searchText) && searchText == "*")
                    {
                        _logger.LogInformation("Ignoring filters because searchText is '*'");
                        searchOptions.Filter = null;
                    }

                    _logger.LogInformation(
                        "SearchOptions: IncludeTotalCount={IncludeTotalCount}, Size={Size}, Filter={Filter}",
                        searchOptions.IncludeTotalCount, searchOptions.Size, searchOptions.Filter);

                    var resp = await client.SearchAsync<T>("*", searchOptions);

                    var items = new List<T>();
                    await foreach (var r in resp.Value.GetResultsAsync())
                    {
                        items.Add(r.Document!);
                        _logger.LogInformation("Fetched document: {Document}", r.Document);
                    }

                    return new AzureSearchResults<T>
                    {
                        Items = items,
                        TotalCount = resp.Value.TotalCount.GetValueOrDefault()
                    };
                }
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure Search failed for '{IndexName}'", indexName);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in SearchAsync for '{IndexName}'", indexName);
                throw;
            }
        }


        private string GetDocumentKey<T>(T document)
        {
            
            var idProperty = typeof(T).GetProperty("Id");
            if (idProperty != null)
            {
                return idProperty.GetValue(document)?.ToString() ?? string.Empty;
            }

           
            var keyProperty = typeof(T).GetProperty("Key");
            if (keyProperty != null)
            {
                return keyProperty.GetValue(document)?.ToString() ?? string.Empty;
            }

            
            if (document is IDictionary<string, object> dict)
            {
                if (dict.ContainsKey("id")) return dict["id"]?.ToString() ?? string.Empty;
                if (dict.ContainsKey("Id")) return dict["Id"]?.ToString() ?? string.Empty;
                if (dict.ContainsKey("key")) return dict["key"]?.ToString() ?? string.Empty;
                if (dict.ContainsKey("Key")) return dict["Key"]?.ToString() ?? string.Empty;
            }

            _logger.LogWarning("Could not extract key from document of type {Type}", typeof(T).Name);
            return string.Empty;
        }

        // Helper method to build filter for multiple keys
        private string BuildFilterForKeys(List<string> keys)
        {
            if (keys.Count == 0) return string.Empty;

            // Replace "id" with your actual key field name in the index
            var keyFieldName = "id"; // Change this to match your index key field

            if (keys.Count == 1)
            {
                return $"{keyFieldName} eq '{keys[0]}'";
            }

            // For multiple keys, use 'or' conditions
            var filterConditions = keys.Select(key => $"{keyFieldName} eq '{key}'");
            return string.Join(" or ", filterConditions);
        }


        public async Task<string> UploadAsync<T>(string IndexName, T document)
        {
            var client = GetClient(IndexName);
            var batch = IndexDocumentsBatch.Upload(new[] { document! });
            await client.IndexDocumentsAsync(batch);
            return $"Document indexed to '{IndexName}'";
        }

        public async Task<T?> GetByIdAsync<T>(string IndexName, string key)
        {
            var client = GetClient(IndexName);
            try
            {
                var resp = await client.GetDocumentAsync<T>(key);
                return resp.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return default;
            }
        }

        public async Task DeleteAsync(string IndexName, string key)
        {
            var client = GetClient(IndexName);
            var batch = IndexDocumentsBatch.Delete("Id", new[] { key });
            await client.IndexDocumentsAsync(batch);
        }
    }
}
