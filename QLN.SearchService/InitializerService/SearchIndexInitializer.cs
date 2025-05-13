using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Azure;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using AzureFieldBuilder = Azure.Search.Documents.Indexes.FieldBuilder;
using QLN.SearchService.IndexModels;

namespace QLN.SearchService.InitializerService
{
    public class SearchIndexInitializer
    {
        private readonly AzureSearchSettings _settings;
        public SearchIndexInitializer(IOptions<AzureSearchSettings> options)
            => _settings = options.Value;

        public async Task EnsureIndexExistsAsync()
        {
            var endpoint = new Uri(_settings.Endpoint);
            var credential = new AzureKeyCredential(_settings.ApiKey);
            var adminClient = new SearchIndexClient(endpoint, credential);
            var indexName = _settings.IndexName;

            // if already there, skip
            await foreach (var name in adminClient.GetIndexNamesAsync())
                if (name == indexName) return;

            // build fields from your POCO
            var builder = new AzureFieldBuilder();
            var searchFields = builder.Build(typeof(ClassifiedIndex));

            // simple, no semantic configuration
            var definition = new SearchIndex(indexName, searchFields);

            await adminClient.CreateIndexAsync(definition);
        }
    }
}
