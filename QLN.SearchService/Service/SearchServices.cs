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
using Azure;

namespace QLN.SearchService.Service
{
    public class SearchServices : ISearchService
    {
        private readonly ISearchRepository _repo;
        private readonly ILogger<SearchServices> _logger;

        private static readonly HashSet<string> DateFilterKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CreatedAt", "PublishedDate", "ExpiryDate",
            "CreatedDate", "PublishedAt", "ExpiredAt"
        };

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

            if (req == null)
                throw new ArgumentNullException(nameof(req), "Search request cannot be null.");

            try
            {
                var (regularFilters, jsonFilters) = await SeparateFiltersAsync(req.Filters, indexName);

                return indexName.Trim().ToLowerInvariant() switch
                {
                    ConstantValues.IndexNames.ClassifiedsItemsIndex => await HandleSearchWithJsonFilters<ClassifiedsItemsIndex>(
                        indexName, req,
                        new List<string> { "IsActive eq true", "Status eq 'Published'" },
                        regularFilters, jsonFilters,
                        (response, items) => response.ClassifiedsItem = items),

                    ConstantValues.IndexNames.ClassifiedsPrelovedIndex => await HandleSearchWithJsonFilters<ClassifiedsPrelovedIndex>(
                        indexName, req,
                        new List<string> { "IsActive eq true", "Status eq 'Published'" },
                        regularFilters, jsonFilters,
                        (response, items) => response.ClassifiedsPrelovedItem = items),

                    ConstantValues.IndexNames.ClassifiedsCollectiblesIndex => await HandleSearchWithJsonFilters<ClassifiedsCollectiblesIndex>(
                        indexName, req,
                        new List<string> { "IsActive eq true", "Status eq 'Published'" },
                        regularFilters, jsonFilters,
                        (response, items) => response.ClassifiedsCollectiblesItem = items),

                    ConstantValues.IndexNames.ClassifiedsDealsIndex => await HandleSearchWithJsonFilters<ClassifiedsDealsIndex>(
                        indexName, req,
                        new List<string> { "IsActive eq true" },
                        regularFilters, jsonFilters,
                        (response, items) => response.ClassifiedsDealsItem = items),

                    ConstantValues.IndexNames.ServicesIndex => await HandleSearchWithJsonFilters<ServicesIndex>(
                        indexName, req,
                        new List<string> { "IsActive eq true", "Status eq 'Published'" },
                        regularFilters, jsonFilters,
                        (response, items) => response.ServicesItems = items),

                    _ => throw new NotSupportedException($"Unknown indexName '{indexName}'")
                };
            }
            catch (ArgumentException ex) when (ex.Message.Contains("Invalid filter") || ex.Message.Contains("Invalid date"))
            {
                _logger.LogWarning(ex, "Invalid filter or date provided for index '{IndexName}'", indexName);
                throw;
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (NotSupportedException)
            {
                throw;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure Search service error for index '{IndexName}'", indexName);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during search for index '{IndexName}'", indexName);
                throw new InvalidOperationException($"Search operation failed for index '{indexName}'. Please try again.", ex);
            }
        }

        public async Task<CommonSearchResponse> GetAllAsync(string indexName, CommonSearchRequest req)
        {
            if (string.IsNullOrWhiteSpace(indexName))
                throw new ArgumentException("IndexName is required.", nameof(indexName));

            if (req == null)
                throw new ArgumentNullException(nameof(req), "Search request cannot be null.");

            try
            {
                var (regularFilters, jsonFilters) = await SeparateFiltersAsync(req.Filters, indexName);

                var modifiedRequest = req;

                if (!string.IsNullOrWhiteSpace(req.Text))
                {
                    var searchTerm = req.Text.Trim();
                    var searchDetection = DetectSearchType(searchTerm);

                    if (searchDetection.Type != SearchType.General && !string.IsNullOrEmpty(searchDetection.Filter))
                    {
                        if (regularFilters == null)
                            regularFilters = new Dictionary<string, object>();

                        if (searchDetection.Filter.Contains("search.ismatch"))
                        {
                            modifiedRequest = new CommonSearchRequest
                            {
                                Text = searchDetection.SearchTerm,
                                Filters = req.Filters,
                                PageNumber = req.PageNumber,
                                PageSize = req.PageSize,
                                OrderBy = req.OrderBy
                            };
                        }
                        else
                        {
                            var filterParts = searchDetection.Filter.Split(new[] { " eq " }, StringSplitOptions.RemoveEmptyEntries);
                            if (filterParts.Length == 2)
                            {
                                var fieldName = filterParts[0].Trim();
                                var fieldValue = filterParts[1].Trim().Trim('\'');
                                regularFilters[fieldName] = fieldValue;
                            }

                            modifiedRequest = new CommonSearchRequest
                            {
                                Text = "*",
                                Filters = req.Filters,
                                PageNumber = req.PageNumber,
                                PageSize = req.PageSize,
                                OrderBy = req.OrderBy
                            };
                        }
                    }
                    else
                    {
                        // UPDATED: Better partial search logic for general searches
                        // Instead of using wildcards in search text, we'll use search.ismatch in filter
                        if (regularFilters == null)
                            regularFilters = new Dictionary<string, object>();

                        // Add partial search filter using search.ismatch
                        regularFilters["_partialSearch"] = searchTerm;

                        modifiedRequest = new CommonSearchRequest
                        {
                            Text = "*", // Use wildcard as main search text
                            Filters = req.Filters,
                            PageNumber = req.PageNumber,
                            PageSize = req.PageSize,
                            OrderBy = req.OrderBy
                        };

                        _logger.LogInformation("Using partial search filter for term: '{SearchTerm}' in index: '{IndexName}'",
                            searchTerm, indexName);
                    }
                }
                else
                {
                    modifiedRequest = new CommonSearchRequest
                    {
                        Text = "*",
                        Filters = req.Filters,
                        PageNumber = req.PageNumber,
                        PageSize = req.PageSize,
                        OrderBy = req.OrderBy
                    };
                }

                return indexName.Trim().ToLowerInvariant() switch
                {
                    ConstantValues.IndexNames.ClassifiedsItemsIndex => await HandleSearchWithJsonFilters<ClassifiedsItemsIndex>(
                        indexName, modifiedRequest,
                        new List<string> { "IsActive eq true" },
                        regularFilters, jsonFilters,
                        (response, items) => response.ClassifiedsItem = items,
                        true),

                    ConstantValues.IndexNames.ClassifiedsPrelovedIndex => await HandleSearchWithJsonFilters<ClassifiedsPrelovedIndex>(
                        indexName, modifiedRequest,
                        new List<string> { "IsActive eq true" },
                        regularFilters, jsonFilters,
                        (response, items) => response.ClassifiedsPrelovedItem = items,
                        true),

                    ConstantValues.IndexNames.ClassifiedsCollectiblesIndex => await HandleSearchWithJsonFilters<ClassifiedsCollectiblesIndex>(
                        indexName, modifiedRequest,
                        new List<string> { "IsActive eq true" },
                        regularFilters, jsonFilters,
                        (response, items) => response.ClassifiedsCollectiblesItem = items,
                        true),

                    ConstantValues.IndexNames.ClassifiedsDealsIndex => await HandleSearchWithJsonFilters<ClassifiedsDealsIndex>(
                        indexName, modifiedRequest,
                        new List<string> { "IsActive eq true" },
                        regularFilters, jsonFilters,
                        (response, items) => response.ClassifiedsDealsItem = items,
                        true),

                    ConstantValues.IndexNames.ServicesIndex => await HandleSearchWithJsonFilters<ServicesIndex>(
                        indexName, modifiedRequest,
                        new List<string> { "IsActive eq true" },
                        regularFilters, jsonFilters,
                        (response, items) => response.ServicesItems = items,
                        true),

                    _ => throw new NotSupportedException($"Unknown indexName '{indexName}'")
                };
            }
            catch (ArgumentException ex) when (ex.Message.Contains("Invalid filter") || ex.Message.Contains("Invalid date"))
            {
                _logger.LogWarning(ex, "Invalid filter or date provided for index '{IndexName}'", indexName);
                throw;
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (NotSupportedException)
            {
                throw;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure Search service error for index '{IndexName}'", indexName);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during GetAll for index '{IndexName}'", indexName);
                throw new InvalidOperationException($"GetAll operation failed for index '{indexName}'. Please try again.", ex);
            }
        }
        private SearchDetectionResult DetectSearchType(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return new SearchDetectionResult
                {
                    Type = SearchType.General,
                    SearchTerm = searchTerm,
                    Filter = string.Empty
                };
            }

            searchTerm = searchTerm.Trim();

            // Check if it's a complete Ad ID (GUID format)
            if (IsAdId(searchTerm))
            {
                return new SearchDetectionResult
                {
                    Type = SearchType.AdId,
                    SearchTerm = searchTerm,
                    Filter = $"Id eq '{searchTerm.Replace("'", "''")}'"
                };
            }

            // Check if it's a partial Ad ID (GUID-like pattern)
            if (IsPartialAdId(searchTerm))
            {
                return new SearchDetectionResult
                {
                    Type = SearchType.AdId,
                    SearchTerm = searchTerm,
                    Filter = $"search.ismatch('{searchTerm.Replace("'", "''")}')" // Search across all fields including Id
                };
            }

            // Check if it's an email format - search in searchable fields
            if (IsEmail(searchTerm))
            {
                return new SearchDetectionResult
                {
                    Type = SearchType.Email,
                    SearchTerm = searchTerm,
                    Filter = $"search.ismatch('{searchTerm.Replace("'", "''")}')" // Search across all searchable fields
                };
            }

            // Check if it's a partial email
            if (IsPartialEmail(searchTerm))
            {
                return new SearchDetectionResult
                {
                    Type = SearchType.Email,
                    SearchTerm = searchTerm,
                    Filter = $"search.ismatch('{searchTerm.Replace("'", "''")}')" // Search across all searchable fields
                };
            }

            // Check if it's a phone number format - search in searchable fields
            if (IsPhoneNumber(searchTerm))
            {
                return new SearchDetectionResult
                {
                    Type = SearchType.PhoneNumber,
                    SearchTerm = searchTerm,
                    Filter = $"search.ismatch('{searchTerm.Replace("'", "''")}')" // Search across all searchable fields
                };
            }

            // Check if it's a partial phone number
            if (IsPartialPhoneNumber(searchTerm))
            {
                return new SearchDetectionResult
                {
                    Type = SearchType.PhoneNumber,
                    SearchTerm = searchTerm,
                    Filter = $"search.ismatch('{searchTerm.Replace("'", "''")}')" // Search across all searchable fields
                };
            }

            // Check if it looks like a UserId (numeric) - search in UserId field specifically
            if (IsUserId(searchTerm))
            {
                return new SearchDetectionResult
                {
                    Type = SearchType.Username,
                    SearchTerm = searchTerm,
                    Filter = $"UserId eq '{searchTerm.Replace("'", "''")}'" // Exact match for UserId
                };
            }

            // Check if it's a username pattern - could be in UserName field
            if (IsUsername(searchTerm))
            {
                return new SearchDetectionResult
                {
                    Type = SearchType.Username,
                    SearchTerm = searchTerm,
                    Filter = $"search.ismatch('{searchTerm.Replace("'", "''")}')" // Search across all searchable fields including UserName
                };
            }

            // Default to general search
            return new SearchDetectionResult
            {
                Type = SearchType.General,
                SearchTerm = searchTerm,
                Filter = string.Empty
            };
        }

        private bool IsUserId(string input)
        {
            // Check if it's a numeric UserId (like "8353026" from your data)
            if (string.IsNullOrWhiteSpace(input)) return false;

            // Check if it's purely numeric and reasonable length for a user ID
            return input.All(char.IsDigit) && input.Length >= 4 && input.Length <= 15;
        }

        private bool IsAdId(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;

            return Guid.TryParse(input, out _);
        }

        private bool IsPartialAdId(string input)
        {
            // Check if it looks like a partial GUID (hexadecimal characters with possible dashes)
            if (string.IsNullOrWhiteSpace(input) || input.Length < 4) return false;

            // Remove dashes and check if it's hexadecimal
            var cleaned = input.Replace("-", "");
            if (cleaned.Length < 4 || cleaned.Length > 32) return false;

            // Check if all characters are hexadecimal
            return cleaned.All(c => char.IsDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'));
        }

        private bool IsOrderId(string input)
        {
            if (input.Length < 3) return false;

            var orderPrefixes = new[] { "ORD", "ord", "ORDER", "order" };
            return orderPrefixes.Any(prefix => input.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                                              input.Substring(prefix.Length).All(char.IsDigit));
        }

        private bool IsPartialOrderId(string input)
        {
            if (string.IsNullOrWhiteSpace(input) || input.Length < 2) return false;

            var orderPrefixes = new[] { "ORD", "ord", "ORDER", "order" };

            return orderPrefixes.Any(prefix =>
                prefix.StartsWith(input, StringComparison.OrdinalIgnoreCase) ||
                input.StartsWith(prefix.Substring(0, Math.Min(prefix.Length, input.Length)), StringComparison.OrdinalIgnoreCase));
        }

        private bool IsEmail(string input)
        {
            try
            {
                var emailRegex = new System.Text.RegularExpressions.Regex(
                    @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                return emailRegex.IsMatch(input);
            }
            catch
            {
                return false;
            }
        }

        private bool IsPartialEmail(string input)
        {
            if (string.IsNullOrWhiteSpace(input) || input.Length < 2) return false;

            if (input.Contains("@")) return true;

            if (input.Contains(".") && input.All(c => char.IsLetterOrDigit(c) || c == '.' || c == '-'))
                return true;

            return input.All(c => char.IsLetterOrDigit(c) || c == '.' || c == '_' || c == '%' || c == '+' || c == '-');
        }

        private bool IsPhoneNumber(string input)
        {
            var cleaned = System.Text.RegularExpressions.Regex.Replace(input, @"[\s\-\(\)\+]", "");

            return cleaned.All(char.IsDigit) && cleaned.Length >= 7 && cleaned.Length <= 15;
        }

        private bool IsPartialPhoneNumber(string input)
        {
            if (string.IsNullOrWhiteSpace(input) || input.Length < 3) return false;

            var cleaned = System.Text.RegularExpressions.Regex.Replace(input, @"[\s\-\(\)\+]", "");

            return cleaned.Length >= 3 && cleaned.Length <= 15 &&
                   cleaned.Count(char.IsDigit) >= (cleaned.Length * 0.7); 
        }

        private bool IsUsername(string input)
        {
            if (input.Length < 2 || input.Length > 50) return false;

            var usernameRegex = new System.Text.RegularExpressions.Regex(@"^[a-zA-Z0-9_-\s]+$");
            return usernameRegex.IsMatch(input) &&
                   !IsEmail(input) &&
                   !IsPhoneNumber(input) &&
                   !IsAdId(input) &&
                   !IsUserId(input) && 
                   !IsPartialEmail(input) &&
                   !IsPartialPhoneNumber(input) &&
                   !IsPartialAdId(input);
        }

        private async Task<CommonSearchResponse> HandleSearchWithJsonFilters<T>(
            string indexName,
            CommonSearchRequest req,
            List<string> baseFilterClauses,
            Dictionary<string, object> regularFilters,
            Dictionary<string, object> jsonFilters,
            Action<CommonSearchResponse, List<T>> assignResults,
            bool isGetAllMethod = false) where T : class
        {
            var response = new CommonSearchResponse();

            try
            {
                if (regularFilters?.Any() == true)
                {
                    var clauses = regularFilters.Select(kv => BuildClause<T>(kv.Key, kv.Value))
                                              .Where(clause => !string.IsNullOrEmpty(clause));
                    baseFilterClauses.AddRange(clauses);
                }

                var filterString = string.Join(" and ", baseFilterClauses);
                bool hasPaging = req.PageNumber > 0 && req.PageSize > 0;

                if (hasPaging)
                {
                    if (req.PageNumber <= 0)
                        throw new ArgumentException("PageNumber must be greater than 0.", nameof(req.PageNumber));

                    if (req.PageSize <= 0 || req.PageSize > 1000)
                        throw new ArgumentException("PageSize must be between 1 and 1000.", nameof(req.PageSize));
                }

                _logger.LogInformation("Applied filter for {IndexName}: {Filter}, SearchText: '{SearchText}'",
                    indexName, filterString, req.Text);

                if (jsonFilters?.Any() == true)
                {
                    var allResultsOpts = new SearchOptions
                    {
                        IncludeTotalCount = true,
                        SearchMode = SearchMode.Any,
                        Filter = filterString,
                        Size = int.MaxValue,
                        QueryType = SearchQueryType.Simple
                    };

                    BuildOrderBy<T>(allResultsOpts, req.OrderBy);

                    var allResults = await _repo.SearchAsync<T>(indexName, allResultsOpts, req.Text);
                    var allFilteredItems = ApplyJsonFilters(allResults.Items, jsonFilters);

                    response.TotalCount = allFilteredItems.Count();

                    var skip = hasPaging ? (req.PageNumber - 1) * req.PageSize : 0;
                    var take = hasPaging ? req.PageSize : int.MaxValue;
                    var paginatedItems = allFilteredItems.Skip(skip).Take(take).ToList();

                    assignResults(response, paginatedItems);
                }
                else
                {
                    var paginatedOpts = new SearchOptions
                    {
                        IncludeTotalCount = true,
                        SearchMode = SearchMode.Any,
                        Filter = filterString,
                        Skip = hasPaging ? (req.PageNumber - 1) * req.PageSize : 0,
                        Size = hasPaging ? req.PageSize : int.MaxValue,
                        QueryType = SearchQueryType.Simple
                    };

                    BuildOrderBy<T>(paginatedOpts, req.OrderBy);

                    var paginatedResult = await _repo.SearchAsync<T>(indexName, paginatedOpts, req.Text);
                    response.TotalCount = (int)paginatedResult.TotalCount;

                    assignResults(response, paginatedResult.Items.ToList());
                }

                return response;
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure Search request failed for index '{IndexName}' with filter: {Filter}, SearchText: '{SearchText}'",
                    indexName, string.Join(" and ", baseFilterClauses), req.Text);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in HandleSearchWithJsonFilters for index '{IndexName}'", indexName);
                throw new InvalidOperationException($"Search operation failed unexpectedly for index '{indexName}'.", ex);
            }
        }

        private async Task<(Dictionary<string, object> regularFilters, Dictionary<string, object> jsonFilters)>
            SeparateFiltersAsync(Dictionary<string, object> filters, string indexName)
        {
            if (filters == null || !filters.Any())
                return (new Dictionary<string, object>(), new Dictionary<string, object>());

            var regularFilters = new Dictionary<string, object>();
            var jsonFilters = new Dictionary<string, object>();

            try
            {
                var knownJsonKeys = await GetKnownJsonKeysFromSampleData(indexName);

                foreach (var filter in filters)
                {
                    if (string.IsNullOrWhiteSpace(filter.Key))
                    {
                        throw new ArgumentException("Filter key cannot be null or empty.", nameof(filters));
                    }

                    if (filter.Value == null)
                    {
                        _logger.LogWarning("Skipping filter '{Key}' with null value", filter.Key);
                        continue;
                    }

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
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error separating filters for index '{IndexName}'", indexName);
                throw new ArgumentException($"Error processing filters for index '{indexName}'. Please check your filter syntax.", ex);
            }
        }

        private bool IsKnownModelProperty(string key, string indexName)
        {
            try
            {
                var modelType = GetModelTypeForVertical(indexName);
                if (modelType == null) return false;

                if (key.Equals("minPrice", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("maxPrice", StringComparison.OrdinalIgnoreCase))
                    return true;

                if (DateFilterKeys.Contains(key))
                    return true;

                var properties = modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                return properties.Any(p => p.Name.Equals(key, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking model property '{Key}' for index '{IndexName}'", key, indexName);
                return false;
            }
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
                    Filter = "IsActive eq true"
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
                            catch (JsonException ex)
                            {
                                _logger.LogWarning(ex, "Invalid JSON in AttributesJson for sample discovery");
                            }
                        }
                    }
                }

                _logger.LogDebug("Discovered {Count} JSON keys for indexName {IndexName}: {Keys}",
                    knownKeys.Count, indexName, string.Join(", ", knownKeys));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to discover JSON keys for indexName {IndexName}, continuing with empty set", indexName);
            }

            return knownKeys;
        }

        private string GetAttributesJsonFromItem(object item)
        {
            if (item == null) return null;

            try
            {
                var type = item.GetType();
                var attributesJsonProperty = type.GetProperty("AttributesJson", BindingFlags.Public | BindingFlags.Instance);
                return attributesJsonProperty?.GetValue(item)?.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting AttributesJson from item");
                return null;
            }
        }

        private void BuildOrderBy<T>(SearchOptions opts, string clientOrderBy)
        {
            opts.OrderBy.Clear();
            var addedFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
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
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error building order by clause, using default ordering");

                opts.OrderBy.Clear();
                opts.OrderBy.Add("CreatedAt desc");
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
            {
                _logger.LogWarning("Type {TypeName} does not have AttributesJson property, skipping JSON filters", type.Name);
                return items;
            }

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
                    _logger.LogWarning(ex, "Failed to parse AttributesJson for item filtering, excluding item");
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

            try
            {
                if (jsonValue.Type == JTokenType.Array)
                {
                    var arrayValues = jsonValue.ToObject<string[]>() ?? Array.Empty<string>();
                    return arrayValues.Any(val => string.Equals(val, filterStr, StringComparison.OrdinalIgnoreCase));
                }

                var jsonStr = jsonValue.ToString();
                return string.Equals(jsonStr, filterStr, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error matching JSON filter for value: {Value}", filterValue);
                return false;
            }
        }

        public async Task<string> UploadAsync(CommonIndexRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var index = request.IndexName?
                              .ToLowerInvariant()
                          ?? throw new ArgumentException("IndexName is required", nameof(request.IndexName));

            try
            {
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

                    default:
                        throw new ArgumentException($"Unsupported Index: '{index}'", nameof(request.IndexName));
                }
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure Search upload failed for index '{IndexName}'", index);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during upload for index '{IndexName}'", index);
                throw new InvalidOperationException($"Upload operation failed for index '{index}'. Please try again.", ex);
            }
        }

        public async Task<T?> GetByIdAsync<T>(string indexName, string key)
        {
            if (string.IsNullOrWhiteSpace(indexName))
                throw new ArgumentException("IndexName is required.", nameof(indexName));
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key is required.", nameof(key));

            try
            {
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
            catch (ArgumentException)
            {
                throw;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure Search GetById failed for index '{IndexName}', key '{Key}'", indexName, key);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during GetById for index '{IndexName}', key '{Key}'", indexName, key);
                throw new InvalidOperationException($"GetById operation failed for index '{indexName}', key '{key}'. Please try again.", ex);
            }
        }

        private string BuildClause<T>(string key, object val)
        {
            try
            {
                // ADDED: Handle partial search filter
                if (key == "_partialSearch")
                {
                    var searchTerm = val?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        // Create search.ismatch filter for partial text search
                        var searchFields = "Title,Description,Brand,Model,Category,L1Category,L2Category,Location,Condition,Color,UserName";
                        return $"search.ismatch('{searchTerm.Replace("'", "''")}*', '{searchFields}', 'simple', 'any')";
                    }
                    return ""; // Return empty if no search term
                }

                if (val is System.Collections.IEnumerable ie && val is not string)
                {
                    var parts = new List<string>();
                    foreach (var item in ie)
                    {
                        if (item != null)
                            parts.Add(BuildClause<T>(key, item));
                    }

                    if (!parts.Any())
                        throw new ArgumentException($"Empty collection provided for filter '{key}'.");

                    return "(" + string.Join(" or ", parts) + ")";
                }

                if (val is JsonElement jeArr && jeArr.ValueKind == JsonValueKind.Array)
                {
                    var parts = jeArr.EnumerateArray()
                                     .Select(elem => BuildClause<T>(key, elem))
                                     .ToArray();

                    if (!parts.Any())
                        throw new ArgumentException($"Empty JSON array provided for filter '{key}'.");

                    return "(" + string.Join(" or ", parts) + ")";
                }

                var isMin = key.Equals("minPrice", StringComparison.OrdinalIgnoreCase);
                var isMax = key.Equals("maxPrice", StringComparison.OrdinalIgnoreCase);

                if (IsDateFilter(key))
                {
                    return BuildDateFilterClause<T>(key, val);
                }

                var prop = typeof(T)
                    .GetProperties()
                    .FirstOrDefault(p => p.Name.Equals(key, StringComparison.OrdinalIgnoreCase));

                var field = prop?.Name ?? key;

                if (isMin || isMax)
                {
                    var raw = FormatRawValue(val);
                    if (string.IsNullOrEmpty(raw))
                        throw new ArgumentException($"Invalid price value for filter '{key}': {val}");

                    return isMin ? $"Price ge {raw}" : $"Price le {raw}";
                }

                switch (val)
                {
                    case JsonElement je:
                        switch (je.ValueKind)
                        {
                            case JsonValueKind.String:
                                var s = je.GetString();
                                if (s == null)
                                    throw new ArgumentException($"Null string value for filter '{key}'.");
                                return $"{field} eq '{s.Replace("'", "''")}'";
                            case JsonValueKind.True:
                            case JsonValueKind.False:
                                return $"{field} eq {je.GetBoolean().ToString().ToLower()}";
                            case JsonValueKind.Number:
                                return $"{field} eq {je.GetRawText()}";
                            default:
                                throw new ArgumentException($"Unsupported JSON value type '{je.ValueKind}' for filter '{key}'.");
                        }
                        break;
                    case string str:
                        if (string.IsNullOrEmpty(str))
                            throw new ArgumentException($"Empty or null string value for filter '{key}'.");
                        return $"{field} eq '{str.Replace("'", "''")}'";
                    case bool b:
                        return $"{field} eq {b.ToString().ToLower()}";
                    case int i:
                        return $"{field} eq {i}";
                    case long l:
                        return $"{field} eq {l}";
                    case double d:
                        if (double.IsNaN(d) || double.IsInfinity(d))
                            throw new ArgumentException($"Invalid double value for filter '{key}': {d}");
                        return $"{field} eq {d.ToString(CultureInfo.InvariantCulture)}";
                    case decimal m:
                        return $"{field} eq {m.ToString(CultureInfo.InvariantCulture)}";
                    default:
                        throw new ArgumentException($"Unsupported filter value type '{val.GetType().Name}' for filter '{key}'. Supported types: string, bool, int, long, double, decimal, JsonElement.");
                }
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building filter clause for key '{Key}', value '{Value}'", key, val);
                throw new ArgumentException($"Error building filter clause for '{key}'. Please check the filter value format.", ex);
            }
        }
        private bool IsDateFilter(string key)
        {
            return DateFilterKeys.Contains(key);
        }

        private string BuildDateFilterClause<T>(string key, object val)
        {
            try
            {
                var dateValue = ParseDateValue(val);
                if (!dateValue.HasValue)
                {
                    throw new ArgumentException($"Invalid date value for filter '{key}': {val}. Expected format: yyyy-MM-dd or yyyy-MM-ddTHH:mm:ss");
                }

                var date = dateValue.Value.Date;
                var nextDate = date.AddDays(1);

                var fieldName = MapDateFilterToField(key);

                var startDate = date.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
                var endDate = nextDate.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

                return $"({fieldName} ge {startDate} and {fieldName} lt {endDate})";
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building date filter clause for key '{Key}', value '{Value}'", key, val);
                throw new ArgumentException($"Error processing date filter '{key}'. Please use a valid date format (yyyy-MM-dd or yyyy-MM-ddTHH:mm:ss).", ex);
            }
        }

        private string MapDateFilterToField(string filterKey)
        {
            return filterKey.ToLowerInvariant() switch
            {
                "createdat" or "createddate" => "CreatedAt",
                "publisheddate" or "publishedat" => "PublishedDate",
                "expirydate" or "expiredat" => "ExpiryDate",
                _ => filterKey
            };
        }

        private DateTime? ParseDateValue(object val)
        {
            if (val == null) return null;

            try
            {
                switch (val)
                {
                    case DateTime dt:
                        return dt;
                    case DateTimeOffset dto:
                        return dto.DateTime;
                    case string str:
                        if (string.IsNullOrWhiteSpace(str))
                            return null;

                        if (DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                            return parsedDate;
                        if (DateTimeOffset.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDateOffset))
                            return parsedDateOffset.DateTime;
                        break;
                    case JsonElement je:
                        if (je.ValueKind == JsonValueKind.String)
                        {
                            var dateStr = je.GetString();
                            if (!string.IsNullOrWhiteSpace(dateStr) &&
                                DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var jeDate))
                                return jeDate;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing date value: {Value}", val);
            }

            return null;
        }

        private string ParseOrderBy<T>(string orderBy)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(orderBy))
                    throw new ArgumentException("OrderBy expression cannot be empty.");

                var parts = orderBy.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var key = parts[0];
                var dir = parts.Length > 1 ? parts[1] : null;

                if (dir != null && !dir.Equals("asc", StringComparison.OrdinalIgnoreCase) &&
                    !dir.Equals("desc", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException($"Invalid sort direction '{dir}'. Use 'asc' or 'desc'.");
                }

                var prop = typeof(T)
                    .GetProperties()
                    .FirstOrDefault(p => p.Name.Equals(key, StringComparison.OrdinalIgnoreCase));

                var field = prop?.Name ?? key;

                return dir != null ? $"{field} {dir}" : field;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing OrderBy expression: {OrderBy}", orderBy);
                throw new ArgumentException($"Invalid OrderBy expression '{orderBy}'. Use format: 'FieldName asc|desc'.", ex);
            }
        }

        private string FormatRawValue(object val)
        {
            try
            {
                if (val is JsonElement je)
                {
                    if (je.ValueKind == JsonValueKind.Number) return je.GetRawText();
                    if (je.ValueKind == JsonValueKind.String) return je.GetString()!;
                }
                return Convert.ToString(val, CultureInfo.InvariantCulture)!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error formatting raw value: {Value}", val);
                throw new ArgumentException($"Error formatting filter value: {val}", ex);
            }
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
                    throw new ArgumentException($"Unknown index: {indexName}", nameof(indexName));

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
            catch (ArgumentException)
            {
                throw;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure Search delete failed for index '{IndexName}', key '{Key}'", indexName, key);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during delete for index '{IndexName}', key '{Key}'", indexName, key);
                throw new InvalidOperationException($"Delete operation failed for index '{indexName}', key '{key}'. Please try again.", ex);
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
            if (similarPageSize <= 0 || similarPageSize > 100)
                throw new ArgumentException("SimilarPageSize must be between 1 and 100.", nameof(similarPageSize));

            try
            {
                var detail = await _repo.GetByIdAsync<T>(indexName, key);

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
                var propL2 = type.GetProperty("L2CategoryId", BindingFlags.Public | BindingFlags.Instance);
                var propL1 = type.GetProperty("L1CategoryId", BindingFlags.Public | BindingFlags.Instance);
                var l2Value = propL2?.GetValue(detail)?.ToString();
                var l1Value = propL1?.GetValue(detail)?.ToString();

                var useL2 = !string.IsNullOrWhiteSpace(l2Value);
                var filterField = useL2 ? "L2CategoryId" : "L1CategoryId";
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
            catch (ArgumentException)
            {
                throw;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure Search GetByIdWithSimilar failed for index '{IndexName}', key '{Key}'", indexName, key);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during GetByIdWithSimilar for index '{IndexName}', key '{Key}'", indexName, key);
                throw new InvalidOperationException($"GetByIdWithSimilar operation failed for index '{indexName}', key '{key}'. Please try again.", ex);
            }
        }
    }
}