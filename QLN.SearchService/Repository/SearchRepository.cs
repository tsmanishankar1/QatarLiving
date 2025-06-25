// File: QLN.SearchService.Repository/SearchRepository.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QLN.Common.DTO_s;
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
                throw new ArgumentException($"No index configured for '{vertical}'", nameof(vertical));
            return new SearchClient(_endpoint, indexName, _credential);
        }

        public async Task<AzureSearchResults<T>> SearchAsync<T>(
            string vertical,
            SearchOptions options,
            string searchText)
        {
            var client = GetClient(vertical);
            try
            {
                _logger.LogInformation("Searching '{Vertical}' for '{Text}'", vertical, searchText ?? "*");
                var resp = await client.SearchAsync<T>(searchText ?? "*", options);

                // pull out documents
                var items = new List<T>();
                await foreach (var r in resp.Value.GetResultsAsync())
                    items.Add(r.Document!);

                return new AzureSearchResults<T>
                {
                    Items = items,
                    TotalCount = resp.Value.TotalCount.GetValueOrDefault()
                };
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure Search failed for '{Vertical}'", vertical);
                throw;
            }
        }

        public async Task<string> UploadAsync<T>(string vertical, T document)
        {
            var client = GetClient(vertical);
            var batch = IndexDocumentsBatch.Upload(new[] { document! });
            await client.IndexDocumentsAsync(batch);
            return $"Document indexed to '{vertical}'";
        }

        public async Task<T?> GetByIdAsync<T>(string vertical, string key)
        {
            var client = GetClient(vertical);
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

        public async Task DeleteAsync(string vertical, string key)
        {
            var client = GetClient(vertical);
            var batch = IndexDocumentsBatch.Delete("Id", new[] { key });
            await client.IndexDocumentsAsync(batch);
        }
    }
}
