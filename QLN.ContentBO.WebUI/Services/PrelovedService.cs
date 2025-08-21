using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Services.Base;
using System.Net;
using System.Text;
using System.Text.Json;

namespace QLN.ContentBO.WebUI.Services
{
    public class PrelovedService : ServiceBase<PrelovedService>, IPrelovedService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;

        public PrelovedService(HttpClient httpClient, ILogger<PrelovedService> Logger)
            : base(httpClient, Logger)
        {
            _httpClient = httpClient;
            _logger = Logger;
        }

        public async Task<HttpResponseMessage?> GetPrelovedP2pListing(PrelovedP2PSubscriptionQuery query)
        {
            try
            {
                var queryParams = new Dictionary<string, string?>
                {
                    { "status", query.Status },
                    { "createdDate", query.CreatedDate },
                    { "publishedDate", query.PublishedDate },
                    { "Page", query.Page.ToString() },
                    { "PageSize", query.PageSize.ToString() },
                    { "Search", query.Search },
                    { "SortBy", query.SortBy },
                    { "SortOrder", query.SortOrder }
                };

                var queryString = string.Join("&",
                    queryParams
                    .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Value))
                    .Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value!)}"));

                var requestUrl = $"/api/v2/classifiedbo/preloved-p2p-subscriptions?{queryString}";

                var response = await _httpClient.GetAsync(requestUrl);

                var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

                return await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetPrelovedP2pListing");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage?> GetPrelovedP2PTransaction(PrelovedP2PTransactionQuery query)
        {
            try
            {
                var baseUrl = "/api/v2/classifiedbo/preloved/transactions";
                var queryParams = new List<string>();
                if (query.Page.HasValue)
                    queryParams.Add($"pageNumber={query.Page}");

                if (query.PageSize.HasValue)
                    queryParams.Add($"pageSize={query.PageSize}");

                if (!string.IsNullOrWhiteSpace(query.Search))
                    queryParams.Add($"searchText={Uri.EscapeDataString(query.Search)}");

                if (!string.IsNullOrWhiteSpace(query.CreatedDate))
                    queryParams.Add($"dateCreated={Uri.EscapeDataString(query.CreatedDate)}");

                if (!string.IsNullOrWhiteSpace(query.PublishedDate))
                    queryParams.Add($"datePublished={Uri.EscapeDataString(query.PublishedDate)}");

                if (!string.IsNullOrWhiteSpace(query.Status))
                    queryParams.Add($"status={Uri.EscapeDataString(query.Status)}");

                if (!string.IsNullOrWhiteSpace(query.SortBy))
                    queryParams.Add($"sortBy={Uri.EscapeDataString(query.SortBy)}");

                if (!string.IsNullOrWhiteSpace(query.SortOrder))
                    queryParams.Add($"sortOrder={Uri.EscapeDataString(query.SortOrder)}");

                var requestUrl = queryParams.Count > 0
                    ? $"{baseUrl}?{string.Join("&", queryParams)}"
                    : baseUrl;

                _logger.LogInformation("Final Request URL: {RequestUrl}", requestUrl);
                var response = await _httpClient.GetAsync(requestUrl);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetPrelovedP2PTransaction");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage?> GetPrelovedSubscription(PrelovedSubscriptionQuery query)
        {
            try
            {
                var queryParams = new Dictionary<string, string?>
                {
                    { "subscriptionType", query.SubscriptionType },
                    { "filterDate", query.FilterDate },
                    { "Page", query.Page.ToString() },
                    { "PageSize", query.PageSize.ToString() },
                    { "Search", query.Search }
                };

                var queryString = string.Join("&", queryParams
                    .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Value))
                    .Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value!)}"));

                var requestUrl = $"/api/v2/classifiedbo/preloved-view-subscriptions?{queryString}";

                var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

                return await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetPrelovedSubscription");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public Task<HttpResponseMessage?> GetPrelovedUserListing(FilterRequest request)
        {
            throw new NotImplementedException();
        }

        public async Task<HttpResponseMessage?> BulkActionAsync(List<long?> adIds, int adStatus)
        {
            try
            {
                var payload = new
                {
                    AdIds = adIds,
                    AdStatus = adStatus
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var requestUrl = "/api/v2/classifiedbo/bulk-preloved-action";
                return await _httpClient.PostAsync(requestUrl, content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BulkActionAsync");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
    }
}
