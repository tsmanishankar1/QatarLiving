using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Options;
using QLN.SearchService.IndexModels;
using QLN.SearchService.IRepository;
using QLN.SearchService.Models;

namespace QLN.SearchService.Repository
{
    public class SearchRepository : ISearchRepository
    {
        private readonly AzureSearchSettings _settings;
        private readonly AzureKeyCredential _cred;
        private readonly Uri _endpoint;

        public SearchRepository(IOptions<AzureSearchSettings> opts)
        {
            _settings = opts.Value;
            _endpoint = new Uri(_settings.Endpoint);
            _cred = new AzureKeyCredential(_settings.ApiKey);
        }

        private SearchClient GetClient(string vertical)
        {
            if (!_settings.Indexes.TryGetValue(vertical, out var indexName))
                throw new ArgumentException($"No index configured for '{vertical}'");

            return new SearchClient(_endpoint, indexName, _cred);
        }

        public async Task<IEnumerable<T>> SearchAsync<T>(string vertical, SearchRequest req)
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
                    var field = kv.Key;
                    var val = kv.Value;
                    string clause;

                    if (val is JsonElement je)
                    {
                        switch (je.ValueKind)
                        {
                            case JsonValueKind.String:
                                var s = je.GetString();
                                clause = $"{field} eq '{s?.Replace("'", "''")}'";
                                break;
                            case JsonValueKind.True:
                            case JsonValueKind.False:
                                var b = je.GetBoolean();
                                clause = $"{field} eq {b.ToString().ToLower()}";
                                break;
                            case JsonValueKind.Number:
                                // preserve numeric format
                                var numText = je.GetRawText();
                                clause = $"{field} eq {numText}";
                                break;
                            default:
                                throw new NotSupportedException(
                                    $"Filter on JSON element kind '{je.ValueKind}' not supported");
                        }
                    }
                    else
                    {
                        clause = val switch
                        {
                            string s => $"{field} eq '{s.Replace("'", "''")}'",
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
                Console.WriteLine($"[SearchRepository] OData filter: {options.Filter}");
            }

            if (!string.IsNullOrWhiteSpace(req.OrderBy))
            {
                options.OrderBy.Add(req.OrderBy);
            }

            var response = await client.SearchAsync<T>(req.Text ?? "*", options);
            return response.Value.GetResults().Select(r => r.Document!);
        }

        public async Task<string> UploadAsync<T>(string vertical, T doc)
        {
            var client = GetClient(vertical);
            var batch = IndexDocumentsBatch.Upload(new[] { doc! });
            await client.IndexDocumentsAsync(batch);
            return $"Document indexed to '{vertical}'";
        }
        public async Task<T> GetByIdAsync<T>(string vertical, string key)
        {
            var client = GetClient(vertical);
            var resp = await client.GetDocumentAsync<T>(key);
            return resp.Value;
        }
    }
}