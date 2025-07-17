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

        public async Task<CommonSearchResponse> SearchAsync(string vertical, string? subVertical, CommonSearchRequest req)
        {
            if (string.IsNullOrWhiteSpace(vertical))
                throw new ArgumentException("Vertical is required.", nameof(vertical));

            var indexKey = subVertical?.Trim().ToLowerInvariant()
                          ?? vertical.Trim().ToLowerInvariant();

            var hasPaging = req.PageNumber > 0 && req.PageSize > 0;
            var opts = new SearchOptions
            {
                IncludeTotalCount = true,
                SearchMode = SearchMode.All,
                Skip = hasPaging ? (req.PageNumber - 1) * req.PageSize : 0,
                Size = hasPaging ? req.PageSize : int.MaxValue
            };

            var response = new CommonSearchResponse
            {
                VerticalName = vertical,
            };

            switch (indexKey)
            {
                case "classifiedsitems":
                    {
                        var clauses = BuildFilter<ClassifiedsItemsIndex>(req.Filters);
                        opts.Filter = string.Join(" and ", clauses);
                        opts.OrderBy.Add("IsPromoted desc");
                        opts.OrderBy.Add("PromotedExpiryDate desc");
                        opts.OrderBy.Add("IsRefreshed desc");
                        opts.OrderBy.Add("RefreshExpiryDate desc");
                        opts.OrderBy.Add("IsFeatured desc");
                        opts.OrderBy.Add("FeaturedExpiryDate desc");
                        opts.OrderBy.Add("CreatedDate desc");

                        var page = await _repo.SearchAsync<ClassifiedsItemsIndex>(indexKey, opts, req.Text);
                        response.TotalCount = page.TotalCount;
                        response.Items = page.Items.ToList();
                        break;
                    }

                case "classifiedspre":
                    {
                        var clauses = BuildFilter<ClassifiedsPrelovedIndex>(req.Filters);
                        opts.Filter = string.Join(" and ", clauses);
                        opts.OrderBy.Add("IsPromoted desc");
                        opts.OrderBy.Add("PromotedExpiryDate desc");
                        opts.OrderBy.Add("IsRefreshed desc");
                        opts.OrderBy.Add("RefreshExpiryDate desc");
                        opts.OrderBy.Add("IsFeatured desc");
                        opts.OrderBy.Add("FeaturedExpiryDate desc");
                        opts.OrderBy.Add("CreatedDate desc");

                        var page = await _repo.SearchAsync<ClassifiedsPrelovedIndex>(indexKey, opts, req.Text);
                        response.TotalCount = page.TotalCount;
                        response.Preloved = page.Items.ToList();
                        break;
                    }

                case "classifiedscollect":
                    {
                        var clauses = BuildFilter<ClassifiedsCollectiblesIndex>(req.Filters);
                        opts.Filter = string.Join(" and ", clauses);
                        opts.OrderBy.Add("CreatedDate desc");

                        var page = await _repo.SearchAsync<ClassifiedsCollectiblesIndex>(indexKey, opts, req.Text);
                        response.TotalCount = page.TotalCount;
                        response.Collectibles = page.Items.ToList();
                        break;
                    }

                case "classifiedsdeals":
                    {
                        var clauses = BuildFilter<ClassifiedsDealsIndex>(req.Filters);
                        opts.Filter = string.Join(" and ", clauses);
                        opts.OrderBy.Add("StartDate desc");

                        var page = await _repo.SearchAsync<ClassifiedsDealsIndex>(indexKey, opts, req.Text);
                        response.TotalCount = page.TotalCount;
                        response.Deals = page.Items.ToList();
                        break;
                    }

                case "services":
                    {
                        var clauses = BuildFilter<ServicesIndex>(req.Filters);
                        opts.Filter = string.Join(" and ", clauses);
                        opts.OrderBy.Add("IsPromoted desc");
                        opts.OrderBy.Add("CreatedDate desc");

                        var page = await _repo.SearchAsync<ServicesIndex>(indexKey, opts, req.Text);
                        response.TotalCount = page.TotalCount;
                        response.ServicesItems = page.Items.ToList();
                        break;
                    }

                default:
                    throw new NotSupportedException($"Unknown or unsupported vertical/subVertical: '{indexKey}'");
            }

            return response;
        }

        public async Task<string> UploadAsync(CommonIndexRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var indexKey = request.SubVertical?.Trim().ToLowerInvariant()
                           ?? request.VerticalName?.Trim().ToLowerInvariant()
                           ?? throw new ArgumentException("VerticalName or SubVertical is required");

            switch (indexKey)
            {
                case "classifiedsitems":
                    var items = request.Items as ClassifiedsItemsIndex
                        ?? throw new ArgumentException("ClassifiedsItem is required for classifiedsitems.");
                    _logger.LogInformation("Uploading ClassifiedsItemsIndex Id={Id} to '{Vertical}'", items.Id, indexKey);
                    return await _repo.UploadAsync(indexKey, items);

                case "classifiedspre":
                    var pre = request.Preloved as ClassifiedsPrelovedIndex
                        ?? throw new ArgumentException("PrelovedItem is required for classifieds-preloved.");
                    _logger.LogInformation("Uploading ClassifiedsPrelovedIndex Id={Id} to '{Vertical}'", pre.Id, indexKey);
                    return await _repo.UploadAsync(indexKey, pre);

                case "classifiedscollect":
                    var collect = request.Collectibles as ClassifiedsCollectiblesIndex
                        ?? throw new ArgumentException("CollectiblesItem is required for classifieds-collectibles.");
                    _logger.LogInformation("Uploading ClassifiedsCollectiblesIndex Id={Id} to '{Vertical}'", collect.Id, indexKey);
                    return await _repo.UploadAsync(indexKey, collect);

                case "classifiedsdeals":
                    var deal = request.Deals as ClassifiedsDealsIndex
                        ?? throw new ArgumentException("DealsItem is required for classifieds-deals.");
                    _logger.LogInformation("Uploading ClassifiedsDealsIndex Id={Id} to '{Vertical}'", deal.Id, indexKey);
                    return await _repo.UploadAsync(indexKey, deal);

                case "services":
                    var svc = request.ServicesItem
                        ?? throw new ArgumentException("ServicesItem is required for services.");
                    _logger.LogInformation("Uploading ServicesIndex Id={Id} to '{Vertical}'", svc.Id, indexKey);
                    return await _repo.UploadAsync(indexKey, svc);

                case "landingbackoffice":
                    var master = request.MasterItem
                        ?? throw new ArgumentException("MasterItem is required for backoffice.");
                    _logger.LogInformation("Uploading LandingBackOfficeIndex Id={Id} to '{Vertical}'", master.Id, indexKey);
                    return await _repo.UploadAsync(indexKey, master);

                default:
                    throw new ArgumentException($"Unsupported vertical or subVertical: '{indexKey}'");
            }
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
