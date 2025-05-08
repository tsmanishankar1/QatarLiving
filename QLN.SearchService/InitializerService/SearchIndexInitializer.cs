using Azure;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Indexes;
using Microsoft.Extensions.Options;
using QLN.SearchService.IndexModels;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace QLN.SearchService.InitializerService
{
    public class SearchIndexInitializer
    {
        private readonly AzureSearchSettings _settings;
        public SearchIndexInitializer(IOptions<AzureSearchSettings> options)
        {
            _settings = options.Value;
        }

        public async Task EnsureIndexExistsAsync()
        {
            var endpoint = new Uri(_settings.Endpoint);
            var credential = new AzureKeyCredential(_settings.ApiKey);
            var adminClient = new SearchIndexClient(endpoint, credential);
            var indexName = _settings.IndexName;

            // Get existing indexes using proper AsyncPageable handling
            var existingIndexes = new List<string>();
            await foreach (var indexNameItem in adminClient.GetIndexNamesAsync())
            {
                existingIndexes.Add(indexNameItem);
            }

            if (existingIndexes.Any(i => i == indexName))
                return;

            var fieldBuilder = new FieldBuilder();
            var searchFields = fieldBuilder.Build(typeof(ClassifiedIndex));
            var definition = new SearchIndex(indexName, searchFields);
            await adminClient.CreateIndexAsync(definition);
        }
    }
}