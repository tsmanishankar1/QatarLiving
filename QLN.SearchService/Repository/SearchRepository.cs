using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QLN.SearchService.IndexModels;
using QLN.SearchService.IRepository;
using QLN.SearchService.Models;

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

        public async Task<IEnumerable<T>> Search<T>(string vertical, SearchRequest req)
        {
            var client = GetClient(vertical);
            var options = new SearchOptions
            {
                Size = req.Top > 0 ? req.Top : 50
            };

            if (req.Filters != null && req.Filters.Any())
            {
                var clauses = new List<string>();

                foreach (var kv in req.Filters)
                {
                    var key = kv.Key;
                    var val = kv.Value;

                    var isMin = key.Equals("minPrice", StringComparison.OrdinalIgnoreCase);
                    var isMax = key.Equals("maxPrice", StringComparison.OrdinalIgnoreCase);

                    var field = (isMin || isMax)
                        ? typeof(T)
                            .GetProperties()
                            .FirstOrDefault(p => p.Name.Equals("Price", StringComparison.OrdinalIgnoreCase))
                            ?.Name
                          ?? "price"
                        : typeof(T)
                            .GetProperties()
                            .FirstOrDefault(p => p.Name.Equals(key, StringComparison.OrdinalIgnoreCase))
                            ?.Name
                          ?? key;

                    string clause;
                    if (isMin || isMax)
                    {
                        string raw;
                        if (val is JsonElement je)
                        {
                            if (je.ValueKind == JsonValueKind.Number)
                                raw = je.GetRawText();        
                            else if (je.ValueKind == JsonValueKind.String)
                                raw = je.GetString()!;       
                            else
                                throw new NotSupportedException(
                                    $"Range filter JSON kind '{je.ValueKind}' not supported");
                        }
                        else
                        {
                            raw = Convert.ToString(val, CultureInfo.InvariantCulture)!;
                        }

                        clause = isMin
                            ? $"{field} ge {raw}"
                            : $"{field} le {raw}";
                    }
                    else if (val is JsonElement je)
                    {
                        switch (je.ValueKind)
                        {
                            case JsonValueKind.String:
                                var s = je.GetString()!.Replace("'", "''");
                                clause = $"{field} eq '{s}'";
                                break;
                            case JsonValueKind.True:
                            case JsonValueKind.False:
                                clause = $"{field} eq {je.GetBoolean().ToString().ToLower()}";
                                break;
                            case JsonValueKind.Number:
                                clause = $"{field} eq {je.GetRawText()}";
                                break;
                            default:
                                throw new NotSupportedException(
                                    $"Filter on JSON kind '{je.ValueKind}' not supported");
                        }
                    }
                    else
                    {
                        clause = val switch
                        {
                            string str => $"{field} eq '{str.Replace("'", "''")}'",
                            bool b => $"{field} eq {b.ToString().ToLower()}",
                            int i => $"{field} eq {i}",
                            long l => $"{field} eq {l}",
                            double d => $"{field} eq {d.ToString(CultureInfo.InvariantCulture)}",
                            decimal m => $"{field} eq {m.ToString(CultureInfo.InvariantCulture)}",
                            _ => throw new NotSupportedException(
                                    $"Filter on type {val.GetType().Name} not supported")
                        };
                    }

                    clauses.Add(clause);
                }

                options.Filter = string.Join(" and ", clauses);
                _logger.LogInformation("Applied OData filter: {Filter}", options.Filter);
            }

            if (!string.IsNullOrWhiteSpace(req.OrderBy))
            {
                var parts = req.OrderBy.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var key = parts[0];
                var dir = parts.Length > 1 ? parts[1] : null;

                var prop = typeof(T)
                                  .GetProperties()
                                  .FirstOrDefault(p => p.Name.Equals(key, StringComparison.OrdinalIgnoreCase));
                var fieldExpr = prop?.Name ?? key;
                var orderExpr = dir != null
                    ? $"{fieldExpr} {dir}"
                    : fieldExpr;

                options.OrderBy.Add(orderExpr);
                _logger.LogInformation("Applying OrderBy: {OrderBy}", orderExpr);
            }

            try
            {
                _logger.LogInformation(
                    "Executing search on '{Vertical}' with text='{Text}'", vertical, req.Text ?? "*");
                var response = await client.SearchAsync<T>(req.Text ?? "*", options);

                var results = response.Value.GetResults()
                    .Select(r => r.Document!)
                    .ToList();

                _logger.LogInformation(
                    "Found {Count} documents in '{Vertical}'", results.Count, vertical);
                return results;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure Search request failed for vertical '{Vertical}'", vertical);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during SearchAsync('{Vertical}')", vertical);
                throw;
            }
        }
        public async Task<string> Upload<T>(string vertical, T doc)
        {
            var client = GetClient(vertical);

            try
            {
                _logger.LogInformation("Indexing document into '{Vertical}'", vertical);
                var batch = IndexDocumentsBatch.Upload(new[] { doc! });
                await client.IndexDocumentsAsync(batch);

                _logger.LogInformation("Successfully indexed document into '{Vertical}'", vertical);
                return $"Document indexed to '{vertical}'";
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure Search failed to index document into '{Vertical}'", vertical);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during UploadAsync('{Vertical}')", vertical);
                throw;
            }
        }

        public async Task<T?> GetById<T>(string vertical, string key)
        {
            var client = GetClient(vertical);

            try
            {
                _logger.LogInformation("Retrieving document '{Key}' from '{Vertical}'", key, vertical);
                var resp = await client.GetDocumentAsync<T>(key);
                _logger.LogInformation("Successfully retrieved document '{Key}'", key);
                return resp.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                _logger.LogWarning("Document '{Key}' not found in '{Vertical}'", key, vertical);
                return default;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during GetByIdAsync('{Key}')", key);
                throw;
            }
        }
    }
}
