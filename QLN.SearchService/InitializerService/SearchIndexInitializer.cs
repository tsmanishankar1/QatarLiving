using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Azure;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s;
using QLN.Common.DTOs;

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

            // 1) Clean out non-alphanumeric: "backoffice-master" → "backofficemaster"
            var cleaned = Regex.Replace(vertical, @"[^A-Za-z0-9]", "");

            // 2) Pascal-case + "Index": "BackofficemasterIndex"
            var pascal = CultureInfo.InvariantCulture.TextInfo
                                 .ToTitleCase(cleaned.ToLowerInvariant());
            var candidate = pascal + "Index";

            _logger.LogDebug("Looking for any type named '{Candidate}' in loaded DTO assemblies", candidate);

            // 3) Scan every loaded type in the DTO assembly for a matching class name
            var type = _modelsAssembly
                .GetTypes()
                .FirstOrDefault(t =>
                    string.Equals(t.Name, candidate, StringComparison.OrdinalIgnoreCase)
                );

            if (type == null)
            {
                var msg = $"No index model found for vertical '{vertical}'. " +
                          $"Expected a class named '{candidate}' in DTO assemblies.";
                _logger.LogError(msg);
                throw new InvalidOperationException(msg);
            }

            return type;
        }
    }
}
