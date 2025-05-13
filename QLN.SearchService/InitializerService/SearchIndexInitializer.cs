using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using Azure;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Microsoft.Extensions.Configuration;
using QLN.SearchService.IndexModels;

namespace QLN.SearchService
{
    public interface ISearchIndexInitializer
    {
        Task InitializeAsync();
    }

    public class SearchIndexInitializer : ISearchIndexInitializer
    {
        private readonly SearchIndexClient _indexClient;
        private readonly IConfiguration _config;
        private readonly Assembly _modelsAssembly;

        public SearchIndexInitializer(SearchIndexClient indexClient, IConfiguration config)
        {
            _indexClient = indexClient;
            _config = config;
            _modelsAssembly = typeof(ClassifiedIndex).Assembly;
        }

        public async Task InitializeAsync()
        {
            var indexes = _config.GetSection("AzureSearch:Indexes").Get<Dictionary<string, string>>();
            foreach (var kvp in indexes)
            {
                var vertical = kvp.Key;
                var indexName = kvp.Value;

                try
                {
                    await _indexClient.GetIndexAsync(indexName);
                    continue;
                }
                catch (RequestFailedException ex) when (ex.Status == 404)
                {
                }

                var typeName = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(vertical) + "Index";
                var modelTypeFullName = $"QLN.SearchService.IndexModels.{typeName}";
                var modelType = _modelsAssembly.GetType(modelTypeFullName, throwOnError: false);
                if (modelType is null)
                    throw new InvalidOperationException($"Index model '{modelTypeFullName}' not found.");

                var fields = new FieldBuilder().Build(modelType);
                var indexDefinition = new SearchIndex(indexName, fields);
                await _indexClient.CreateIndexAsync(indexDefinition);
            }
        }
    }
}
