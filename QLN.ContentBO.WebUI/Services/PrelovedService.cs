using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Services.Base;
using System.Net;

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
                var queryParams = new Dictionary<string, string?>
                {
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

                var requestUrl = $"/api/v2/classifiedbo/preloved-p2p-transactions?{queryString}";

                var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

                return await _httpClient.SendAsync(request);
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

        public Task<HttpResponseMessage?> PerformPrelovedBulkActionAsync(object payload)
        {
            throw new NotImplementedException();
        }
    }
}
