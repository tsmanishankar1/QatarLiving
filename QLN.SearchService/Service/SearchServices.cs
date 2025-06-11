using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Logging;
using Azure.Search.Documents;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.ISearchService;
using QLN.Common.Infrastructure.IRepository.ISearchServiceRepository;

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

        public async Task<CommonSearchResponse> SearchAsync(string vertical, CommonSearchRequest req)
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
                    .Select(kv => BuildClause<object>(kv.Key, kv.Value)) 
                    .ToList();
                opts.Filter = string.Join(" and ", clauses);
                _logger.LogInformation("Applied filter: {Filter}", opts.Filter);
            }

            if (!string.IsNullOrWhiteSpace(req.OrderBy))
            {
                var orderExpr = ParseOrderBy<object>(req.OrderBy);     
                opts.OrderBy.Add(orderExpr);
                _logger.LogInformation("Applied OrderBy: {OrderBy}", orderExpr);
            }

            var response = new CommonSearchResponse
            {
                VerticalName = vertical
            };

            switch (vertical.Trim().ToLowerInvariant())
            {
                case "classifieds":
                    var classifieds = await _repo.SearchAsync<ClassifiedsIndex>(
                        vertical, opts, req.Text);
                    response.ClassifiedsItems = classifieds.ToList();
                    break;

                case "services":
                    var services = await _repo.SearchAsync<ServicesIndex>(
                        vertical, opts, req.Text);
                    response.ServicesItems = services.ToList();
                    break;

                case "backofficemaster":
                    var masters = await _repo.SearchAsync<BackofficemasterIndex>(
                        vertical, opts, req.Text);
                    response.MasterItems = masters.ToList();
                    break;

                default:
                    throw new NotSupportedException($"Unknown vertical '{vertical}'");
            }

            return response;
        }


        public async Task<string> UploadAsync(CommonIndexRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var vertical = request.VerticalName?
                              .ToLowerInvariant()
                          ?? throw new ArgumentException("VerticalName is required", nameof(request.VerticalName));
            switch (vertical.Trim().ToLowerInvariant())
            {
                case "classifieds":
                    var classifieds = request.ClassifiedsItem
                           ?? throw new ArgumentException("ClassifiedsItem is required for classifieds.", nameof(request.ClassifiedsItem));
                    _logger.LogInformation("Uploading ClassifiedIndex Id={Id} to '{Vertical}'", classifieds.Id, vertical);
                    return await _repo.UploadAsync<ClassifiedsIndex>(vertical, classifieds);

                case "services":
                    var svc = request.ServicesItem
                           ?? throw new ArgumentException("ServicesItem is required for services.", nameof(request.ServicesItem));
                    _logger.LogInformation("Uploading ServicesIndex Id={Id} to '{Vertical}'",
                        svc.Id, vertical);
                    return await _repo.UploadAsync<ServicesIndex>(vertical, svc);

                case "backofficemaster":
                    var master = request.MasterItem
                           ?? throw new ArgumentException("Backoffice item.", nameof(request.MasterItem));
                    _logger.LogInformation("Uploading ClassifiedIndex Id={Id} to '{Vertical}'", master.Id, vertical);
                    return await _repo.UploadAsync<BackofficemasterIndex>(vertical, master);
            }
            throw new ArgumentException($"Unsupported vertical: '{vertical}'", nameof(request.VerticalName));
        }

        public Task<T?> GetByIdAsync<T>(string vertical, string key)
        {
            if (string.IsNullOrWhiteSpace(vertical))
                throw new ArgumentException("Vertical is required.", nameof(vertical));
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key is required.", nameof(key));

            return _repo.GetByIdAsync<T>(vertical, key);
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
        public async Task DeleteAsync(string vertical, string key)
        {
            if (string.IsNullOrWhiteSpace(vertical))
                throw new ArgumentException("Vertical is required.", nameof(vertical));
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key is required.", nameof(key));

            try
            {
                _logger.LogInformation("Service: deleting '{Key}' from '{Vertical}'", key, vertical);
                await _repo.DeleteAsync(vertical, key);
                _logger.LogInformation("Service: deleted '{Key}' from '{Vertical}'", key, vertical);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "DeleteAsync called with invalid argument: {Param}", ex.ParamName);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in DeleteAsync: vertical={Vertical}, key={Key}", vertical, key);
                throw;
            }
        }
    }
}
