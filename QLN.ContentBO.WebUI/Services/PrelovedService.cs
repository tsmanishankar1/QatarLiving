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

        public Task<HttpResponseMessage?> GetPrelovedP2pListing(FilterRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseMessage?> GetPrelovedP2pTransaction(FilterRequest request)
        {
            throw new NotImplementedException();
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
