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
using Microsoft.AspNetCore.Identity;
using QLN.Common.Infrastructure.Constants;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JsonException = Newtonsoft.Json.JsonException;

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

        public async Task<CommonSearchResponse> SearchAsync(string indexName, CommonSearchRequest req)
        {
            if (string.IsNullOrWhiteSpace(indexName))
                throw new ArgumentException("IndexName is required.", nameof(indexName));

            bool hasPaging = req.PageNumber > 0 && req.PageSize > 0;
            var opts = new SearchOptions
            {
                IncludeTotalCount = true,
                SearchMode = SearchMode.All,
                Skip = hasPaging ? (req.PageNumber - 1) * req.PageSize : 0,
                Size = hasPaging ? req.PageSize : int.MaxValue
            };

            var (regularFilters, jsonFilters) = await SeparateFiltersAsync(req.Filters, indexName);

            var response = new CommonSearchResponse();

            switch (indexName.Trim().ToLowerInvariant())
            {
                case ConstantValues.IndexNames.ClassifiedsItemsIndex:
                    {
                        var filterClauses = new List<string> { "IsActive eq true" };

                        if (regularFilters?.Any() == true)
                        {
                            var clauses = regularFilters
                                .Select(kv => BuildClause<ClassifiedsItemsIndex>(kv.Key, kv.Value));
                            filterClauses.AddRange(clauses);
                        }

                        opts.Filter = string.Join(" and ", filterClauses);
                        _logger.LogInformation("Applied filter for classifieds: {Filter}", opts.Filter);

                        BuildOrderBy<ClassifiedsItemsIndex>(opts, req.OrderBy);

                        var pageCls = await _repo.SearchAsync<ClassifiedsItemsIndex>(indexName, opts, req.Text);
                        var filteredItems = ApplyJsonFilters(pageCls.Items, jsonFilters);

                        response.TotalCount = pageCls.TotalCount;
                        response.ClassifiedsItem = filteredItems.ToList();
                        break;
                    }

                case ConstantValues.IndexNames.ClassifiedsPrelovedIndex:
                    {
                        var filterClauses = new List<string> { "IsActive eq true" };

                        if (regularFilters?.Any() == true)
                        {
                            var clauses = regularFilters
                                .Select(kv => BuildClause<ClassifiedsPrelovedIndex>(kv.Key, kv.Value));
                            filterClauses.AddRange(clauses);
                        }

                        opts.Filter = string.Join(" and ", filterClauses);
                        _logger.LogInformation("Applied filter for classifieds: {Filter}", opts.Filter);

                        BuildOrderBy<ClassifiedsPrelovedIndex>(opts, req.OrderBy);

                        var pageCls = await _repo.SearchAsync<ClassifiedsPrelovedIndex>(indexName, opts, req.Text);
                        var filteredItems = ApplyJsonFilters(pageCls.Items, jsonFilters);

                        response.TotalCount = pageCls.TotalCount;
                        response.ClassifiedsPrelovedItem = filteredItems.ToList();
                        break;
                    }

                case ConstantValues.IndexNames.ClassifiedsCollectiblesIndex:
                    {
                        var filterClauses = new List<string> { "IsActive eq true" };

                        if (regularFilters?.Any() == true)
                        {
                            var clauses = regularFilters
                                .Select(kv => BuildClause<ClassifiedsCollectiblesIndex>(kv.Key, kv.Value));
                            filterClauses.AddRange(clauses);
                        }

                        opts.Filter = string.Join(" and ", filterClauses);
                        _logger.LogInformation("Applied filter for classifieds: {Filter}", opts.Filter);

                        BuildOrderBy<ClassifiedsCollectiblesIndex>(opts, req.OrderBy);

                        var pageCls = await _repo.SearchAsync<ClassifiedsCollectiblesIndex>(indexName, opts, req.Text);
                        var filteredItems = ApplyJsonFilters(pageCls.Items, jsonFilters);

                        response.TotalCount = pageCls.TotalCount;
                        response.ClassifiedsCollectiblesItem = filteredItems.ToList();
                        break;
                    }

                case ConstantValues.IndexNames.ClassifiedsDealsIndex:
                    {
                        var filterClauses = new List<string> { "IsActive eq true" };

                        if (regularFilters?.Any() == true)
                        {
                            var clauses = regularFilters
                                .Select(kv => BuildClause<ClassifiedsDealsIndex>(kv.Key, kv.Value));
                            filterClauses.AddRange(clauses);
                        }

                        opts.Filter = string.Join(" and ", filterClauses);
                        _logger.LogInformation("Applied filter for classifieds: {Filter}", opts.Filter);

                        BuildOrderBy<ClassifiedsDealsIndex>(opts, req.OrderBy);

                        var pageCls = await _repo.SearchAsync<ClassifiedsDealsIndex>(indexName, opts, req.Text);
                        var filteredItems = ApplyJsonFilters(pageCls.Items, jsonFilters);

                        response.TotalCount = pageCls.TotalCount;
                        response.ClassifiedsDealsItem = filteredItems.ToList();
                        break;
                    }

                case ConstantValues.IndexNames.ServicesIndex:
                    {
                        var filterClauses = new List<string> { "IsActive eq true" };

                        if (regularFilters?.Any() == true)
                        {
                            var clauses = regularFilters
                                .Select(kv => BuildClause<ServicesIndex>(kv.Key, kv.Value));
                            filterClauses.AddRange(clauses);
                        }

                        opts.Filter = string.Join(" and ", filterClauses);
                        _logger.LogInformation("Applied filter for services: {Filter}", opts.Filter);

                        BuildOrderBy<ServicesIndex>(opts, req.OrderBy);

                        var pageSvc = await _repo.SearchAsync<ServicesIndex>(indexName, opts, req.Text);
                        var filteredItems = ApplyJsonFilters(pageSvc.Items, jsonFilters);

                        response.TotalCount = pageSvc.TotalCount;
                        response.ServicesItems = filteredItems.ToList();
                        break;
                    }

                case ConstantValues.IndexNames.LandingBackOfficeIndex:
                    {
                        var filterClauses = new List<string> { "IsActive eq true" };

                        if (regularFilters?.Any() == true)
                        {
                            var clauses = regularFilters
                                .Select(kv => BuildClause<LandingBackOfficeIndex>(kv.Key, kv.Value));
                            filterClauses.AddRange(clauses);
                        }

                        opts.Filter = string.Join(" and ", filterClauses);
                        _logger.LogInformation("Applied filter for backoffice: {Filter}", opts.Filter);

                        BuildOrderBy<LandingBackOfficeIndex>(opts, req.OrderBy);

                        var pageBo = await _repo.SearchAsync<LandingBackOfficeIndex>(indexName, opts, req.Text);
                        response.TotalCount = pageBo.TotalCount;
                        response.MasterItems = pageBo.Items.ToList();
                        break;
                    }

                default:
                    throw new NotSupportedException($"Unknown indexName '{indexName}'");
            }

            return response;
        }

        public async Task<CommonSearchResponse> GetAllAsync(string indexName, CommonSearchRequest req)
        {
            if (string.IsNullOrWhiteSpace(indexName))
                throw new ArgumentException("IndexName is required.", nameof(indexName));

            bool hasPaging = req.PageNumber > 0 && req.PageSize > 0;
            var opts = new SearchOptions
            {
                IncludeTotalCount = true,
                SearchMode = SearchMode.All,
                Skip = hasPaging ? (req.PageNumber - 1) * req.PageSize : 0,
                Size = hasPaging ? req.PageSize : int.MaxValue
            };

            var (regularFilters, jsonFilters) = await SeparateFiltersAsync(req.Filters, indexName);
            var response = new CommonSearchResponse();

            async Task Handle<T>(Action<List<T>> assign) where T : class
            {
                var filterClauses = new List<string> { "IsActive eq true" };

                if (regularFilters?.Any() == true)
                {
                    var clauses = regularFilters.Select(kv => BuildClause<T>(kv.Key, kv.Value));
                    filterClauses.AddRange(clauses);
                }

                opts.Filter = string.Join(" and ", filterClauses);
                BuildOrderBy<T>(opts, req.OrderBy);
                var result = await _repo.SearchAsync<T>(indexName, opts, req.Text);
                var filtered = ApplyJsonFilters(result.Items, jsonFilters).ToList();

                assign(filtered);
                response.TotalCount = result.TotalCount;
            }

            switch (indexName.Trim().ToLowerInvariant())
            {
                case ConstantValues.IndexNames.ClassifiedsItemsIndex:
                    await Handle<ClassifiedsItemsIndex>(x => response.ClassifiedsItem = x); break;

                case ConstantValues.IndexNames.ClassifiedsPrelovedIndex:
                    await Handle<ClassifiedsPrelovedIndex>(x => response.ClassifiedsPrelovedItem = x); break;

                case ConstantValues.IndexNames.ClassifiedsCollectiblesIndex:
                    await Handle<ClassifiedsCollectiblesIndex>(x => response.ClassifiedsCollectiblesItem = x); break;

                case ConstantValues.IndexNames.ClassifiedsDealsIndex:
                    await Handle<ClassifiedsDealsIndex>(x => response.ClassifiedsDealsItem = x); break;

                case ConstantValues.IndexNames.ServicesIndex:
                    await Handle<ServicesIndex>(x => response.ServicesItems = x); break;

                case ConstantValues.IndexNames.LandingBackOfficeIndex:
                    // Add IsActive filter for LandingBackOfficeIndex as well
                    opts.Filter = "IsActive eq true";
                    if (regularFilters?.Any() == true)
                    {
                        var clauses = regularFilters.Select(kv => BuildClause<LandingBackOfficeIndex>(kv.Key, kv.Value));
                        opts.Filter = "IsActive eq true and " + string.Join(" and ", clauses);
                    }

                    var raw = await _repo.SearchAsync<LandingBackOfficeIndex>(indexName, opts, req.Text);
                    response.TotalCount = raw.TotalCount;
                    response.MasterItems = raw.Items.ToList();
                    break;

                default:
                    throw new NotSupportedException($"Unknown indexName '{indexName}'");
            }

            return response;
        }

        private async Task<(Dictionary<string, object> regularFilters, Dictionary<string, object> jsonFilters)>
            SeparateFiltersAsync(Dictionary<string, object> filters, string indexName)
        {
            if (filters == null || !filters.Any())
                return (new Dictionary<string, object>(), new Dictionary<string, object>());

            var regularFilters = new Dictionary<string, object>();
            var jsonFilters = new Dictionary<string, object>();

            var knownJsonKeys = await GetKnownJsonKeysFromSampleData(indexName);

            foreach (var filter in filters)
            {
                if (IsKnownModelProperty(filter.Key, indexName))
                {
                    regularFilters[filter.Key] = filter.Value;
                }
                else if (knownJsonKeys.Contains(filter.Key, StringComparer.OrdinalIgnoreCase))
                {
                    jsonFilters[filter.Key] = filter.Value;
                }
                else
                {
                    jsonFilters[filter.Key] = filter.Value;
                    _logger.LogDebug("Unknown filter key '{Key}' assumed to be JSON attribute", filter.Key);
                }
            }

            return (regularFilters, jsonFilters);
        }

        private bool IsKnownModelProperty(string key, string indexName)
        {
            var modelType = GetModelTypeForVertical(indexName);
            if (modelType == null) return false;

            if (key.Equals("minPrice", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("maxPrice", StringComparison.OrdinalIgnoreCase))
                return true;

            var properties = modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            return properties.Any(p => p.Name.Equals(key, StringComparison.OrdinalIgnoreCase));
        }

        private Type GetModelTypeForVertical(string indexName)
        {
            return indexName.ToLowerInvariant() switch
            {
                ConstantValues.IndexNames.ClassifiedsItemsIndex => typeof(ClassifiedsItemsIndex),
                ConstantValues.IndexNames.ClassifiedsPrelovedIndex => typeof(ClassifiedsPrelovedIndex),
                ConstantValues.IndexNames.ClassifiedsCollectiblesIndex => typeof(ClassifiedsCollectiblesIndex),
                ConstantValues.IndexNames.ClassifiedsDealsIndex => typeof(ClassifiedsDealsIndex),
                ConstantValues.IndexNames.ServicesIndex => typeof(ServicesIndex),
                ConstantValues.IndexNames.LandingBackOfficeIndex => typeof(LandingBackOfficeIndex),
                _ => null
            };
        }

        private async Task<HashSet<string>> GetKnownJsonKeysFromSampleData(string indexName)
        {
            var knownKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var opts = new SearchOptions
                {
                    Size = 10,
                    IncludeTotalCount = false,
                    Filter = "IsActive eq true" // Only sample from active records
                };

                dynamic sampleResults = indexName.ToLowerInvariant() switch
                {
                    ConstantValues.IndexNames.ClassifiedsItemsIndex =>
                        await _repo.SearchAsync<ClassifiedsItemsIndex>(indexName, opts, "*"),
                    ConstantValues.IndexNames.ClassifiedsPrelovedIndex =>
                        await _repo.SearchAsync<ClassifiedsPrelovedIndex>(indexName, opts, "*"),
                    ConstantValues.IndexNames.ClassifiedsCollectiblesIndex =>
                        await _repo.SearchAsync<ClassifiedsCollectiblesIndex>(indexName, opts, "*"),
                    ConstantValues.IndexNames.ClassifiedsDealsIndex =>
                        await _repo.SearchAsync<ClassifiedsDealsIndex>(indexName, opts, "*"),
                    ConstantValues.IndexNames.ServicesIndex =>
                        await _repo.SearchAsync<ServicesIndex>(indexName, opts, "*"),
                    _ => null
                };

                if (sampleResults?.Items != null)
                {
                    foreach (var item in sampleResults.Items)
                    {
                        var attributesJson = GetAttributesJsonFromItem(item);
                        if (!string.IsNullOrEmpty(attributesJson))
                        {
                            try
                            {
                                var jsonObj = JObject.Parse(attributesJson);
                                foreach (var property in jsonObj.Properties())
                                {
                                    knownKeys.Add(property.Name);
                                }
                            }
                            catch (JsonException)
                            {
                                // Ignore invalid JSON
                            }
                        }
                    }
                }

                _logger.LogDebug("Discovered {Count} JSON keys for indexName {IndexName}: {Keys}",
                    knownKeys.Count, indexName, string.Join(", ", knownKeys));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to discover JSON keys for indexName {IndexName}", indexName);
            }

            return knownKeys;
        }

        private string GetAttributesJsonFromItem(object item)
        {
            if (item == null) return null;

            var type = item.GetType();
            var attributesJsonProperty = type.GetProperty("AttributesJson", BindingFlags.Public | BindingFlags.Instance);
            return attributesJsonProperty?.GetValue(item)?.ToString();
        }

        private void BuildOrderBy<T>(SearchOptions opts, string clientOrderBy)
        {
            opts.OrderBy.Clear();
            var addedFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(clientOrderBy))
            {
                var expr = ParseOrderBy<T>(clientOrderBy);
                var fieldName = ExtractFieldName(expr);

                if (!addedFields.Contains(fieldName))
                {
                    opts.OrderBy.Add(expr);
                    addedFields.Add(fieldName);
                    _logger.LogInformation("Added client sort: {OrderBy}", expr);
                }
            }

            var defaultOrderFields = GetDefaultOrderFields<T>();
            foreach (var orderField in defaultOrderFields)
            {
                var fieldName = ExtractFieldName(orderField);
                if (!addedFields.Contains(fieldName))
                {
                    opts.OrderBy.Add(orderField);
                    addedFields.Add(fieldName);
                }
            }
        }

        private List<string> GetDefaultOrderFields<T>()
        {
            var typeName = typeof(T).Name;

            return typeName switch
            {
                "ClassifiedsItemsIndex" or "ClassifiedsPrelovedIndex" or "ClassifiedsCollectiblesIndex" or "ServicesIndex" =>
                    new List<string>
                    {
                        "IsPromoted desc",
                        "PromotedExpiryDate desc",
                        "IsRefreshed desc",
                        "RefreshExpiryDate desc",
                        "IsFeatured desc",
                        "FeaturedExpiryDate desc",
                        "CreatedAt desc"
                    },

                "ClassifiedsDealsIndex" => new List<string> { "CreatedAt desc" },

                _ => new List<string> { "CreatedAt desc" }
            };
        }

        private string ExtractFieldName(string orderByExpression)
        {
            if (string.IsNullOrWhiteSpace(orderByExpression))
                return string.Empty;

            var parts = orderByExpression.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 0 ? parts[0] : orderByExpression;
        }

        private IEnumerable<T> ApplyJsonFilters<T>(IEnumerable<T> items, Dictionary<string, object> jsonFilters) where T : class
        {
            if (jsonFilters == null || !jsonFilters.Any())
                return items;

            var type = typeof(T);
            var attributesJsonProperty = type.GetProperty("AttributesJson");

            if (attributesJsonProperty == null)
                return items;

            return items.Where(item =>
            {
                var attributesJson = attributesJsonProperty.GetValue(item)?.ToString();
                if (string.IsNullOrEmpty(attributesJson))
                    return false;

                try
                {
                    var attributes = JObject.Parse(attributesJson);

                    foreach (var filter in jsonFilters)
                    {
                        var jsonValue = attributes[filter.Key];
                        var filterValue = filter.Value;

                        if (!MatchesJsonFilter(jsonValue, filterValue))
                        {
                            return false;
                        }
                    }

                    return true;
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse AttributesJson for item filtering");
                    return false;
                }
            });
        }

        private bool MatchesJsonFilter(JToken jsonValue, object filterValue)
        {
            if (jsonValue == null || jsonValue.Type == JTokenType.Null)
                return false;

            var filterStr = filterValue?.ToString();
            if (string.IsNullOrEmpty(filterStr))
                return false;

            if (jsonValue.Type == JTokenType.Array)
            {
                var arrayValues = jsonValue.ToObject<string[]>() ?? Array.Empty<string>();
                return arrayValues.Any(val => string.Equals(val, filterStr, StringComparison.OrdinalIgnoreCase));
            }

            var jsonStr = jsonValue.ToString();
            return string.Equals(jsonStr, filterStr, StringComparison.OrdinalIgnoreCase);
        }

        public async Task<string> UploadAsync(CommonIndexRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var index = request.IndexName?
                              .ToLowerInvariant()
                          ?? throw new ArgumentException("IndexName is required", nameof(request.IndexName));

            switch (index.Trim().ToLowerInvariant())
            {
                case ConstantValues.IndexNames.ClassifiedsItemsIndex:
                    var item = request.ClassifiedsItem
                               ?? throw new ArgumentException("ClassifiedsItem is required for classifiedsitems.");
                    return await _repo.UploadAsync<ClassifiedsItemsIndex>(index, item);

                case ConstantValues.IndexNames.ClassifiedsPrelovedIndex:
                    var preloved = request.ClassifiedsPrelovedItem
                                   ?? throw new ArgumentException("ClassifiedsItem is required for classifiedspreloved.");
                    return await _repo.UploadAsync<ClassifiedsPrelovedIndex>(index, preloved);

                case ConstantValues.IndexNames.ClassifiedsCollectiblesIndex:
                    var collect = request.ClassifiedsCollectiblesItem
                                  ?? throw new ArgumentException("ClassifiedsItem is required for classifiedscollectibles.");
                    return await _repo.UploadAsync<ClassifiedsCollectiblesIndex>(index, collect);

                case ConstantValues.IndexNames.ClassifiedsDealsIndex:
                    var deals = request.ClassifiedsDealsItem
                                ?? throw new ArgumentException("ClassifiedsItem is required for classifiedsdeals.");
                    return await _repo.UploadAsync<ClassifiedsDealsIndex>(index, deals);

                case ConstantValues.IndexNames.ServicesIndex:
                    var svc = request.ServicesItem
                           ?? throw new ArgumentException("ServicesItem is required for services.", nameof(request.ServicesItem));
                    return await _repo.UploadAsync<ServicesIndex>(index, svc);

                case ConstantValues.IndexNames.LandingBackOfficeIndex:
                    var master = request.MasterItem
                           ?? throw new ArgumentException("Backoffice item.", nameof(request.MasterItem));
                    return await _repo.UploadAsync<LandingBackOfficeIndex>(index, master);
            }
            throw new ArgumentException($"Unsupported Index: '{index}'", nameof(request.IndexName));
        }

        public async Task<T?> GetByIdAsync<T>(string indexName, string key)
        {
            if (string.IsNullOrWhiteSpace(indexName))
                throw new ArgumentException("IndexName is required.", nameof(indexName));
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key is required.", nameof(key));

            var item = await _repo.GetByIdAsync<T>(indexName, key);

            if (item != null)
            {
                var type = typeof(T);
                var isActiveProp = type.GetProperty("IsActive", BindingFlags.Public | BindingFlags.Instance);

                if (isActiveProp != null)
                {
                    var isActiveValue = isActiveProp.GetValue(item);

                    if (isActiveValue is bool isActive && !isActive)
                    {
                        return default(T);
                    }
                }
            }

            return item;
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
                return isMin ? $"Price ge {raw}" : $"Price le {raw}";
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

            return dir != null ? $"{field} {dir}" : field;
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

        public async Task DeleteAsync(string indexName, string key)
        {
            if (string.IsNullOrWhiteSpace(indexName))
                throw new ArgumentException("IndexName is required.", nameof(indexName));
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key is required.", nameof(key));

            try
            {
                _logger.LogInformation("Soft-deleting '{Key}' from '{IndexName}'", key, indexName);

                var modelType = GetModelTypeForVertical(indexName);
                if (modelType == null)
                    throw new NotSupportedException($"Unknown index: {indexName}");

                var method = typeof(ISearchRepository).GetMethod(nameof(ISearchRepository.GetByIdAsync))!
                                                        .MakeGenericMethod(modelType);

                var task = (Task)method.Invoke(_repo, new object[] { indexName, key })!;
                await task.ConfigureAwait(false);
                var resultProp = task.GetType().GetProperty("Result")!;
                var doc = resultProp.GetValue(task);
                if (doc == null)
                    throw new KeyNotFoundException($"Document '{key}' not found in '{indexName}'.");

                var prop = modelType.GetProperty("IsActive");
                if (prop == null)
                    throw new InvalidOperationException($"'{modelType.Name}' does not have IsActive property.");

                prop.SetValue(doc, false);

                var uploadMethod = typeof(ISearchRepository).GetMethod(nameof(ISearchRepository.UploadAsync))!
                                                             .MakeGenericMethod(modelType);

                var uploadTask = (Task)uploadMethod.Invoke(_repo, new object[] { indexName, doc! })!;
                await uploadTask.ConfigureAwait(false);

                _logger.LogInformation("Soft-deleted '{Key}' from '{IndexName}'", key, indexName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during soft-delete for {Key} in {IndexName}", key, indexName);
                throw;
            }
        }

        public async Task<GetWithSimilarResponse<T>> GetByIdWithSimilarAsync<T>(
           string indexName,
           string key,
           int similarPageSize = 10
       ) where T : class
        {
            if (string.IsNullOrWhiteSpace(indexName))
                throw new ArgumentException("IndexName is required.", nameof(indexName));
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key is required.", nameof(key));

            var detail = await _repo.GetByIdAsync<T>(indexName, key);

            // Check if the detail item exists and is active
            if (detail != null)
            {
                var types = typeof(T);
                var isActiveProp = types.GetProperty("IsActive", BindingFlags.Public | BindingFlags.Instance);

                if (isActiveProp != null)
                {
                    var isActiveValue = isActiveProp.GetValue(detail);

                    if (isActiveValue is bool isActive && !isActive)
                    {
                        throw new KeyNotFoundException($"No active record with key '{key}' found in '{indexName}'.");
                    }
                }
            }

            if (detail == null)
                throw new KeyNotFoundException($"No '{key}' in '{indexName}'.");

            var type = typeof(T);
            var propL2 = type.GetProperty("L2Category", BindingFlags.Public | BindingFlags.Instance);
            var propL1 = type.GetProperty("L1Category", BindingFlags.Public | BindingFlags.Instance);
            var l2Value = propL2?.GetValue(detail)?.ToString();
            var l1Value = propL1?.GetValue(detail)?.ToString();

            var useL2 = !string.IsNullOrWhiteSpace(l2Value);
            var filterField = useL2 ? "L2Category" : "L1Category";
            var filterValue = useL2 ? l2Value! : l1Value;

            if (string.IsNullOrWhiteSpace(filterValue))
            {
                return new GetWithSimilarResponse<T> { Detail = detail };
            }

            var opts = new SearchOptions
            {
                SearchMode = SearchMode.All,
                IncludeTotalCount = false,
                Size = similarPageSize
            };

            // Add IsActive filter along with category filter
            opts.Filter = $"IsActive eq true and {filterField} eq '{filterValue.Replace("'", "''")}'";

            var simResults = await _repo.SearchAsync<T>(indexName, opts, "*");

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