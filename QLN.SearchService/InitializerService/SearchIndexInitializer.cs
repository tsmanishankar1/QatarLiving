using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using Azure;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<SearchIndexInitializer> _logger;
        private readonly Assembly _modelsAssembly;

        public SearchIndexInitializer(
            SearchIndexClient indexClient,
            IConfiguration config,
            ILogger<SearchIndexInitializer> logger)
        {
            _indexClient = indexClient;
            _config = config;
            _logger = logger;
            _modelsAssembly = typeof(ClassifiedIndex).Assembly;
        }

        public async Task InitializeAsync()
        {
            var indexMap = LoadIndexConfiguration();
            foreach (var (vertical, indexName) in indexMap)
            {
                await EnsureIndexExistsAsync(vertical, indexName);
            }
        }

        private Dictionary<string, string> LoadIndexConfiguration()
        {
            var section = _config.GetSection("AzureSearch:Indexes");
            var dict = section.Get<Dictionary<string, string>>();
            if (dict is null || dict.Count == 0)
            {
                throw new InvalidOperationException(
                    "Missing or empty AzureSearch:Indexes configuration.");
            }

            return dict;
        }

        private async Task EnsureIndexExistsAsync(string vertical, string indexName)
        {
            try
            {
                _logger.LogInformation("Checking for index {IndexName}", indexName);
                await _indexClient.GetIndexAsync(indexName);
                _logger.LogInformation("Index {IndexName} already exists, skipping creation.", indexName);
                return;
            }
            catch (RequestFailedException e) when (e.Status == 404)
            {
                _logger.LogInformation("Index {IndexName} not found, will create.", indexName);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed while checking existence of index {IndexName}", indexName);
                throw;
            }

            // If we get here, the index was 404 – so create it.
            var modelType = ResolveModelType(vertical);
            var fields = new FieldBuilder().Build(modelType);
            var definition = new SearchIndex(indexName, fields);

            try
            {
                _logger.LogInformation("Creating index {IndexName} for vertical {Vertical}", indexName, vertical);
                await _indexClient.CreateIndexAsync(definition);
                _logger.LogInformation("Index {IndexName} created successfully.", indexName);
            }
            catch (RequestFailedException e)
            {
                _logger.LogError(e, "Azure Search failed to create index {IndexName}", indexName);
                throw;
            }
        }

        private Type ResolveModelType(string vertical)
        {
            // Normalize e.g. "classified" => "ClassifiedIndex"
            var typeName = CultureInfo.InvariantCulture
                .TextInfo
                .ToTitleCase(vertical) + "Index";

            var fullName = $"QLN.SearchService.IndexModels.{typeName}";
            var t = _modelsAssembly.GetType(fullName, throwOnError: false);
            if (t is null)
            {
                throw new InvalidOperationException(
                    $"No index model type found for vertical '{vertical}'. " +
                    $"Expected CLR type name: {fullName}");
            }

            return t;
        }
    }
}
