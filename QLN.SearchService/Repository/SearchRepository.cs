using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QLN.Common.Infrastructure.IRepository.ISearchServiceRepository;
using QLN.SearchService.IndexModels;

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

        private SearchClient GetClient(string vertical)
        {
            if (!_settings.Indexes.TryGetValue(vertical, out var indexName))
                throw new ArgumentException($"No index configured for '{vertical}'");
            return new SearchClient(_endpoint, indexName, _credential);
        }

        public async Task<IEnumerable<T>> SearchAsync<T>(
            string vertical,
            SearchOptions options,
            string searchText)
        {
            var client = GetClient(vertical);
            try
            {
                _logger.LogInformation("Executing search on '{Vertical}' with text='{Text}'",
                    vertical, searchText ?? "*");

                var response = await client.SearchAsync<T>(searchText ?? "*", options);
                var results = new List<T>();
                await foreach (var page in response.Value.GetResultsAsync())
                    results.Add(page.Document!);

                _logger.LogInformation("Found {Count} documents in '{Vertical}'", results.Count, vertical);
                return results;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure Search failed for vertical '{Vertical}'", vertical);
                throw;
            }
        }

        public async Task<string> UploadAsync<T>(string vertical, T document)
        {
            var client = GetClient(vertical);
            try
            {
                _logger.LogInformation("Indexing document into '{Vertical}'", vertical);
                var batch = IndexDocumentsBatch.Upload(new[] { document! });
                await client.IndexDocumentsAsync(batch);
                _logger.LogInformation("Indexed document into '{Vertical}'", vertical);
                return $"Document indexed to '{vertical}'";
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure Search upload failed for '{Vertical}'", vertical);
                throw;
            }
        }

        public async Task<T?> GetByIdAsync<T>(string vertical, string key)
        {
            var client = GetClient(vertical);
            try
            {
                _logger.LogInformation("Retrieving document '{Key}' from '{Vertical}'", key, vertical);
                var resp = await client.GetDocumentAsync<T>(key);
                return resp.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                _logger.LogWarning("Document '{Key}' not found in '{Vertical}'", key, vertical);
                return default;
            }
        }
        public async Task DeleteAsync(string vertical, string key)
        {
            var client = GetClient(vertical);
            try
            {
                _logger.LogInformation("Deleting document '{Key}' from '{Vertical}'", key, vertical);

                var batch = IndexDocumentsBatch.Delete("Id", new[] { key });
                await client.IndexDocumentsAsync(batch);

                _logger.LogInformation("Deleted document '{Key}' from '{Vertical}'", key, vertical);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                _logger.LogWarning("Document '{Key}' not found for deletion in '{Vertical}'", key, vertical);
                throw;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure Search delete failed for '{Key}' in '{Vertical}'", key, vertical);
                throw;
            }
        }
    }
}
