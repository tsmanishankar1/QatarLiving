using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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

        private SearchClient GetClient(string IndexName)
        {
            if (!_settings.Indexes.TryGetValue(IndexName, out var indexName))
                throw new ArgumentException($"No index configured for '{IndexName}'", nameof(IndexName));
            return new SearchClient(_endpoint, indexName, _credential);
        }

        public async Task<AzureSearchResults<T>> SearchAsync<T>(
            string IndexName,
            SearchOptions options,
            string searchText)
        {
            var client = GetClient(IndexName);
            try
            {
                var processedSearchText = ProcessSearchTextForPartialMatch(searchText);

                options.SearchMode = SearchMode.Any;
                if (options.QueryType == null)
                {
                    options.QueryType = SearchQueryType.Simple;
                }

                if (!options.HighlightFields.Any())
                {
                    var availableHighlightFields = GetAvailableHighlightFields<T>();
                    foreach (var field in availableHighlightFields)
                    {
                        options.HighlightFields.Add(field);
                    }

                    if (options.HighlightFields.Any())
                    {
                        options.HighlightPreTag = "<mark>";
                        options.HighlightPostTag = "</mark>";
                    }
                }

                _logger.LogInformation("Searching '{IndexName}' for '{Text}' (processed: '{ProcessedText}')",
                    IndexName, searchText ?? "*", processedSearchText);

                var resp = await client.SearchAsync<T>(processedSearchText, options);

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
                _logger.LogError(ex, "Azure Search failed for '{IndexName}'", IndexName);
                throw;
            }
        }

        private string ProcessSearchTextForPartialMatch(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText) || searchText == "*")
                return "*";

            try
            {
                var cleanText = Regex.Replace(searchText, @"[^\w\s*]", "").Trim();

                if (string.IsNullOrWhiteSpace(cleanText))
                    return "*";

                if (cleanText.Contains("*"))
                    return cleanText;

                var terms = cleanText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (terms.Length == 1)
                {
                    var term = terms[0];
                    return term.Length >= 2 ? $"{term}*" : term;
                }
                else
                {

                    var phraseQuery = $"\"{string.Join(" ", terms)}\"";
                    var wildcardTerms = string.Join(" ", terms.Select(t => t.Length >= 2 ? $"{t}*" : t));

                    return $"{phraseQuery} {wildcardTerms}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error processing search text '{SearchText}', using original", searchText);
                return searchText;
            }
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

        private List<string> GetAvailableHighlightFields<T>()
        {
            var typeName = typeof(T).Name;

            return typeName switch
            {
                "ClassifiedsItemsIndex" or "ClassifiedsPrelovedIndex" or "ClassifiedsCollectiblesIndex" =>
                    new List<string> { "Title", "Description", "Brand", "Model" },

                "ClassifiedsDealsIndex" =>
                    new List<string> { "BusinessName", "offertitle", "Description" },

                "ClassifiedStoresIndex" =>
                    new List<string> { "CompanyName", "ProductName", "ProductDescription", "ProductSummary" },

                "ServicesIndex" =>
                    new List<string> { "Title", "Description", "Location" },

                "ContentNewsIndex" =>
                    new List<string> { "Title", "Content" },

                "ContentEventsIndex" =>
                    new List<string> { "EventTitle", "EventDescription", "Location" },

                "ContentCommunityIndex" =>
                    new List<string> { "Title", "Description" },

                "CompanyProfileIndex" =>
                    new List<string> { "CompanyName", "BusinessDescription" },

                _ => new List<string> { "Title" } 
            };
        }

        public async Task<List<string>> GetSuggestionsAsync(string indexName, string searchText, int maxSuggestions = 10)
        {
            var client = GetClient(indexName);

            if (string.IsNullOrWhiteSpace(searchText) || searchText.Length < 2)
                return new List<string>();

            try
            {
                var options = new SearchOptions
                {
                    SearchMode = SearchMode.Any,
                    QueryType = SearchQueryType.Full,
                    Size = maxSuggestions,
                    Select = { "Title", "Name", "Description" }
                };

                var processedText = $"{searchText}*";
                var response = await client.SearchAsync<dynamic>(processedText, options);

                var suggestions = new HashSet<string>();
                await foreach (var result in response.Value.GetResultsAsync())
                {
                    if (result.Document.TryGetValue("Title", out string title) && title != null)
                        suggestions.Add(title.ToString());
                    if (result.Document.TryGetValue("Name", out string name) && name != null)
                        suggestions.Add(name.ToString());

                    if (suggestions.Count >= maxSuggestions) break;
                }

                return suggestions.Take(maxSuggestions).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Suggestions failed for '{IndexName}'", indexName);
                return new List<string>();
            }
        }
    }
}