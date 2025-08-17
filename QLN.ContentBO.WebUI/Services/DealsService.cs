using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Services.Base;
using System.Net;
using System.Text;
using System.Text.Json;

namespace QLN.ContentBO.WebUI.Services
{
    public class DealsService : ServiceBase<DealsService>, IDealsService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        public DealsService(HttpClient httpClient, ILogger<DealsService> Logger)
            : base(httpClient, Logger)
        {
            _httpClient = httpClient;
            _logger = Logger;
        }

        public async Task<HttpResponseMessage?> BulkActionAsync(List<long?> adIds, int action, string? reason = null, string? comments = null)
        {
            try
            {
                var payload = new
                {
                    AdIds = adIds,
                    Action = action,
                    Reason = reason,
                    Comments = comments
                };
                Console.Write("the deals bulk action is" + payload );
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var requestUrl = "/api/v2/classifiedbo/bulk-deals-action";
                return await _httpClient.PostAsync(requestUrl, content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BulkActionAsync");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage> GetAllDealsListing(DealsItemQuery query)
{
    try
    {
        // Print the query params object (serialize for readability)
        var queryJson = JsonSerializer.Serialize(query, new JsonSerializerOptions { WriteIndented = true });
        Console.WriteLine("Query object received:");
        Console.WriteLine(queryJson);

        var queryParams = new Dictionary<string, string?>
        {
            { "pageNumber", query.PageNumber.ToString() },
            { "pageSize", query.PageSize.ToString() },
            { "startDate", query.StartDate },
            { "endDate", query.EndDate },
            { "search", query.Search },
            { "sortBy", query.SortBy },
            { "status", query.Status },
            { "isPromoted", query.IsPromoted?.ToString() },
            { "isFeatured", query.IsFeatured?.ToString() }
        };

        // Build query string by skipping null or empty values
        var queryString = string.Join("&", queryParams
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
            .Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value!)}"));

        var requestUrl = $"/api/v2/classifiedbo/DealsViewSummary{(string.IsNullOrEmpty(queryString) ? "" : "?" + queryString)}";

        // Print final request URL
        Console.WriteLine("Request URL:");
        Console.WriteLine(requestUrl);

        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        return await _httpClient.SendAsync(request);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "GetAllDealsListing");
        return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
    }
}

        public async Task<HttpResponseMessage> GetAllDealsSubscription(DealsSubscriptionQuery query)
        {
            try
            {
                var queryParams = new Dictionary<string, string>
                {
                    { "pageNumber", query.PageNumber.ToString() },
                    { "pageSize", query.PageSize.ToString() },
                    { "subscriptionType", query.SubscriptionType },
                    { "startDate", query.StartDate },
                    { "endDate", query.EndDate },
                    { "search", query.Search },
                    { "sortBy", query.SortBy }
                };

                var queryString = string.Join("&", queryParams
                    .Where(kv => !string.IsNullOrEmpty(kv.Value))
                    .Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));

                var requestUrl = $"/api/v2/classifiedbo/getdealsSummary?{queryString}";

                var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

                return await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAllDealsSubscription");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
    }
}
