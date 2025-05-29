// QLN.SearchService.Service/SearchService.cs
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Logging;
using QLN.SearchService.IRepository;
using QLN.SearchService.IService;
using QLN.SearchService.IndexModels;
using QLN.SearchService.Models;
using Azure.Search.Documents;

namespace QLN.SearchService.Service
{
    public class SearchServices : ISearchService
    {
        private readonly ISearchRepository _repo;
        private readonly ILogger<SearchServices> _logger;

        public SearchServices(
            ISearchRepository repo,
            ILogger<SearchServices> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<IEnumerable<ClassifiedsIndex>> SearchAsync(
            string vertical,
            SearchRequest req)
        {
            if (string.IsNullOrWhiteSpace(vertical))
                throw new ArgumentException("Vertical is required.", nameof(vertical));

            var opts = new SearchOptions
            {
                Size = req.Top > 0 ? req.Top : 50,
                SearchMode = SearchMode.All
            };

            if (req.Filters != null && req.Filters.Any())
            {
                var clauses = req.Filters
                    .Select(kv => BuildClause<ClassifiedsIndex>(kv.Key, kv.Value))
                    .ToList();
                opts.Filter = string.Join(" and ", clauses);
                _logger.LogInformation("Applied filter: {Filter}", opts.Filter);
            }

            if (!string.IsNullOrWhiteSpace(req.OrderBy))
            {
                var orderExpr = ParseOrderBy<ClassifiedsIndex>(req.OrderBy);
                opts.OrderBy.Add(orderExpr);
                _logger.LogInformation("Applied OrderBy: {OrderBy}", orderExpr);
            }

            return await _repo.SearchAsync<ClassifiedsIndex>(
                vertical,
                opts,
                req.Text);
        }

        public async Task<string> UploadAsync(CommonIndexRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var vertical = request.VerticalName?
                              .ToLowerInvariant()
                          ?? throw new ArgumentException("VerticalName is required", nameof(request.VerticalName));

            if (vertical == Constants.Constants.classifieds)
            {
                var item = request.ClassifiedsItem
                           ?? throw new ArgumentException("ClassifiedsItem is required for classifieds.", nameof(request.ClassifiedsItem));
                _logger.LogInformation("Uploading ClassifiedIndex Id={Id} to '{Vertical}'", item.Id, vertical);
                return await _repo.UploadAsync<ClassifiedsIndex>(vertical, item);
            }

            throw new ArgumentException($"Unsupported vertical: '{vertical}'", nameof(request.VerticalName));
        }

        public Task<ClassifiedsIndex?> GetByIdAsync(string vertical, string key)
        {
            if (string.IsNullOrWhiteSpace(vertical))
                throw new ArgumentException("Vertical is required.", nameof(vertical));
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key is required.", nameof(key));

            return _repo.GetByIdAsync<ClassifiedsIndex>(vertical, key);
        }

        private string BuildClause<T>(string key, object val)
        {
            var isMin = key.Equals("minPrice", StringComparison.OrdinalIgnoreCase);
            var isMax = key.Equals("maxPrice", StringComparison.OrdinalIgnoreCase);

            var prop = typeof(T)
                .GetProperties()
                .FirstOrDefault(p => p.Name.Equals(key, StringComparison.OrdinalIgnoreCase));
            var field = prop?.Name ?? key;

            if (isMin || isMax)
            {
                var raw = FormatRawValue(val);
                return isMin
                    ? $"{field} ge {raw}"
                    : $"{field} le {raw}";
            }

            switch (val)
            {
                case JsonElement je:
                    switch (je.ValueKind)
                    {
                        case JsonValueKind.String:
                            var s = je.GetString()!.Replace("'", "''");
                            return $"{field} eq '{s}'";
                        case JsonValueKind.True:
                        case JsonValueKind.False:
                            return $"{field} eq {je.GetBoolean().ToString().ToLower()}";
                        case JsonValueKind.Number:
                            return $"{field} eq {je.GetRawText()}";
                    }
                    break;
                case string str:
                    return $"{field} eq '{str.Replace("'", "''")}'";
                case bool b:
                    return $"{field} eq {b.ToString().ToLower()}";
                case int i:
                    return $"{field} eq {i}";
                case long l:
                    return $"{field} eq {l}";
                case double d:
                    return $"{field} eq {d.ToString(CultureInfo.InvariantCulture)}";
                case decimal m:
                    return $"{field} eq {m.ToString(CultureInfo.InvariantCulture)}";
            }

            throw new NotSupportedException($"Filter on type '{val.GetType().Name}' not supported");
        }

        private string ParseOrderBy<T>(string orderBy)
        {
            var parts = orderBy.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var key = parts[0];
            var dir = parts.Length > 1 ? parts[1] : null;

            var prop = typeof(T)
                .GetProperties()
                .FirstOrDefault(p => p.Name.Equals(key, StringComparison.OrdinalIgnoreCase));
            var field = prop?.Name ?? key;

            return dir != null
                ? $"{field} {dir}"
                : field;
        }

        private string FormatRawValue(object val)
        {
            if (val is JsonElement je)
            {
                if (je.ValueKind == JsonValueKind.Number) return je.GetRawText();
                if (je.ValueKind == JsonValueKind.String) return je.GetString()!;
            }
            return Convert.ToString(val, CultureInfo.InvariantCulture)!;
        }
    }
}
