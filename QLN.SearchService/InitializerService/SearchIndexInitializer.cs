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
using QLN.SearchService.Models;

namespace QLN.SearchService
{
    public interface ISearchIndexInitializer
    {
        /// <summary>
        /// Ensures that all configured search indexes are created (recreated if outdated).
        /// </summary>
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
            _indexClient = indexClient ?? throw new ArgumentNullException(nameof(indexClient));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _modelsAssembly = typeof(ClassifiedsIndex).Assembly;
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
            try
            {
                var section = _config.GetSection("AzureSearch:Indexes");
                var dict = section.Get<Dictionary<string, string>>();
                if (dict == null || dict.Count == 0)
                {
                    var msg = "AzureSearch:Indexes configuration is missing or empty.";
                    _logger.LogError(msg);
                    throw new InvalidOperationException(msg);
                }
                return dict;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading AzureSearch:Indexes configuration.");
                throw;
            }
        }

        private async Task EnsureIndexExistsAsync(string vertical, string indexName)
        {
            try
            {
                _logger.LogInformation("Checking for index '{IndexName}'", indexName);
                await _indexClient.GetIndexAsync(indexName);
                _logger.LogInformation("Index '{IndexName}' already exists, skipping.", indexName);
                return;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                _logger.LogInformation("Index '{IndexName}' not found, creating...", indexName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking index '{IndexName}'", indexName);
                throw;
            }

            try
            {
                var modelType = ResolveModelType(vertical);
                var fields = new FieldBuilder().Build(modelType);
                var definition = new SearchIndex(indexName, fields);

                _logger.LogInformation("Creating index '{IndexName}' for vertical '{Vertical}'", indexName, vertical);
                await _indexClient.CreateIndexAsync(definition);
                _logger.LogInformation("Index '{IndexName}' created successfully.", indexName);
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure Search failed to create index '{IndexName}'", indexName);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating index '{IndexName}'", indexName);
                throw;
            }
        }

        private Type ResolveModelType(string vertical)
        {
            if (string.IsNullOrWhiteSpace(vertical))
            {
                var msg = "Vertical name cannot be null or empty.";
                _logger.LogError(msg);
                throw new ArgumentException(msg, nameof(vertical));
            }

            // Use the vertical name as-is (Title-cased) to build the model type name:
            var typeName = CultureInfo.InvariantCulture
                .TextInfo.ToTitleCase(vertical) + "Index";

            var fullName = $"QLN.SearchService.Models.{typeName}";
            _logger.LogDebug("Resolving CLR type '{FullName}' for vertical '{Vertical}'", fullName, vertical);

            var type = _modelsAssembly.GetType(fullName, throwOnError: false);
            if (type == null)
            {
                var msg = $"No index model found for vertical '{vertical}'. Expected CLR type: {fullName}";
                _logger.LogError(msg);
                throw new InvalidOperationException(msg);
            }
            return type;
        }
    }
}
