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
using System.Reflection;

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

            bool hasPaging = req.PageNumber > 0 && req.PageSize > 0;
            var opts = new SearchOptions
            {
                IncludeTotalCount = true,
                SearchMode = SearchMode.All,
                Skip = hasPaging ? (req.PageNumber - 1) * req.PageSize : 0,
                Size = hasPaging ? req.PageSize : int.MaxValue
            };

            var response = new CommonSearchResponse { VerticalName = vertical };

            switch (vertical.Trim().ToLowerInvariant())
            {
                case "classifieds":
                    {
                        if (req.Filters?.Any() == true)
                        {
                            var clauses = req.Filters
                                .Select(kv => BuildClause<ClassifiedsIndex>(kv.Key, kv.Value));
                            opts.Filter = string.Join(" and ", clauses);
                            _logger.LogInformation("Applied filter for classifieds: {Filter}", opts.Filter);
                        }

                        opts.OrderBy.Clear();
                        if (!string.IsNullOrWhiteSpace(req.OrderBy))
                        {
                            var expr = ParseOrderBy<ClassifiedsIndex>(req.OrderBy);
                            opts.OrderBy.Add(expr);
                            _logger.LogInformation("Appended client sort for classifieds: {OrderBy}", expr);
                        }
                        opts.OrderBy.Add("IsPromoted desc");
                        opts.OrderBy.Add("PromotedExpiryDate desc");
                        opts.OrderBy.Add("IsRefreshed desc");
                        opts.OrderBy.Add("RefreshExpiryDate desc");
                        opts.OrderBy.Add("IsFeatured desc");
                        opts.OrderBy.Add("FeatureExpiryDate desc");
                        opts.OrderBy.Add("CreatedDate desc");

                        var pageCls = await _repo.SearchAsync<ClassifiedsIndex>(vertical, opts, req.Text);
                        response.TotalCount = pageCls.TotalCount;
                        response.ClassifiedsItems = pageCls.Items.ToList();
                        response.SubVertical = response.ClassifiedsItems
                                                    .FirstOrDefault()?
                                                    .SubVertical
                                                    ?? string.Empty;
                        break;
                    }

                case "services":
                    {
                        if (req.Filters?.Any() == true)
                        {
                            var clauses = req.Filters
                                .Select(kv => BuildClause<ServicesIndex>(kv.Key, kv.Value));
                            opts.Filter = string.Join(" and ", clauses);
                            _logger.LogInformation("Applied filter for services: {Filter}", opts.Filter);
                        }

                        opts.OrderBy.Clear();
                        if (!string.IsNullOrWhiteSpace(req.OrderBy))
                        {
                            var expr = ParseOrderBy<ServicesIndex>(req.OrderBy);
                            opts.OrderBy.Add(expr);
                            _logger.LogInformation("Appended client sort for services: {OrderBy}", expr);
                        }
                        opts.OrderBy.Add("IsPromoted desc");
                        opts.OrderBy.Add("PromotedExpiryDate desc");
                        opts.OrderBy.Add("IsRefreshed desc");
                        opts.OrderBy.Add("RefreshExpiryDate desc");
                        opts.OrderBy.Add("IsFeatured desc");
                        opts.OrderBy.Add("FeatureExpiryDate desc");
                        opts.OrderBy.Add("CreatedDate desc");

                        var pageSvc = await _repo.SearchAsync<ServicesIndex>(vertical, opts, req.Text);
                        response.TotalCount = pageSvc.TotalCount;
                        response.ServicesItems = pageSvc.Items.ToList();
                        break;
                    }

                case "landingbackoffice":
                    {
                        if (req.Filters?.Any() == true)
                        {
                            var clauses = req.Filters
                                .Select(kv => BuildClause<LandingBackOfficeIndex>(kv.Key, kv.Value));
                            opts.Filter = string.Join(" and ", clauses);
                            _logger.LogInformation("Applied filter for backoffice: {Filter}", opts.Filter);
                        }

                        opts.OrderBy.Clear();
                        if (!string.IsNullOrWhiteSpace(req.OrderBy))
                        {
                            var expr = ParseOrderBy<LandingBackOfficeIndex>(req.OrderBy);
                            opts.OrderBy.Add(expr);
                            _logger.LogInformation("Applied client sort for backoffice: {OrderBy}", expr);
                        }

                        var pageBo = await _repo.SearchAsync<LandingBackOfficeIndex>(vertical, opts, req.Text);
                        response.TotalCount = pageBo.TotalCount;
                        response.MasterItems = pageBo.Items.ToList();
                        break;
                    }

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

                case "landingbackoffice":
                    var master = request.MasterItem
                           ?? throw new ArgumentException("Backoffice item.", nameof(request.MasterItem));
                    _logger.LogInformation("Uploading LandingBackoffice Id={Id} to '{Vertical}'", master.Id, vertical);
                    return await _repo.UploadAsync<LandingBackOfficeIndex>(vertical, master);
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
            if (val is System.Collections.IEnumerable ie && val is not string)
            {
                var parts = new List<string>();
                foreach (var item in ie)
                    parts.Add(BuildClause<T>(key, item!));
                return "(" + string.Join(" or ", parts) + ")";
            }
            if (val is JsonElement jeArr && jeArr.ValueKind == JsonValueKind.Array)
            {
                var parts = jeArr.EnumerateArray()
                                 .Select(elem => BuildClause<T>(key, elem))
                                 .ToArray();
                return "(" + string.Join(" or ", parts) + ")";
            }

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
                    ? $"Price ge {raw}"
                    : $"Price le {raw}";
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
        public async Task<GetWithSimilarResponse<T>> GetByIdWithSimilarAsync<T>(
           string vertical,
           string key,
           int similarPageSize = 10
       ) where T : class
        {
            if (string.IsNullOrWhiteSpace(vertical))
                throw new ArgumentException("Vertical is required.", nameof(vertical));
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key is required.", nameof(key));

            // 1) fetch the primary document
            var detail = await _repo.GetByIdAsync<T>(vertical, key)
                         ?? throw new KeyNotFoundException($"No '{key}' in '{vertical}'.");

            // 2) reflect out L2Category or fallback to L1Category
            var type = typeof(T);
            var propL2 = type.GetProperty("L2Category", BindingFlags.Public | BindingFlags.Instance);
            var propL1 = type.GetProperty("L1Category", BindingFlags.Public | BindingFlags.Instance);
            var l2Value = propL2?.GetValue(detail)?.ToString();
            var l1Value = propL1?.GetValue(detail)?.ToString();

            var useL2 = !string.IsNullOrWhiteSpace(l2Value);
            var filterField = useL2 ? "L2Category" : "L1Category";
            var filterValue = useL2 ? l2Value! : l1Value;

            // if no category at all, return detail only
            if (string.IsNullOrWhiteSpace(filterValue))
            {
                return new GetWithSimilarResponse<T> { Detail = detail };
            }

            // 3) build a small search to fetch “similar” items
            var opts = new SearchOptions
            {
                SearchMode = SearchMode.All,
                IncludeTotalCount = false,
                Size = similarPageSize
            };
            opts.Filter = $"{filterField} eq '{filterValue.Replace("'", "''")}'";

            var simResults = await _repo.SearchAsync<T>(vertical, opts, "*");

            // 4) exclude the original item
            var idProp = type.GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
            var similar = simResults.Items
                .Where(item =>
                {
                    var idVal = idProp?.GetValue(item)?.ToString();
                    return idVal != key;
                })
                .ToList();

            return new GetWithSimilarResponse<T>
            {
                Detail = detail,
                Similar = similar
            };
        }
    }
}
