using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Services.Base;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net;
using System.Text;

namespace QLN.ContentBO.WebUI.Services
{
    public class StoresService : ServiceBase<StoresService>, IStoresService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;

        public StoresService(HttpClient httpClient, ILogger<StoresService> Logger)
           : base(httpClient, Logger)
        {
            _httpClient = httpClient;
            _logger = Logger;
        }
        public async Task<HttpResponseMessage> GetAllStoresListing(CompanySubscriptionFilter companyRequestPayload)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull 
                };
                var json = JsonSerializer.Serialize(companyRequestPayload, options);
                var request = new HttpRequestMessage(HttpMethod.Post, "/api/companyprofile/viewstores")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                var response = await _httpClient.SendAsync(request);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAllStoresListing");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }


        public async Task<HttpResponseMessage> GetAllStoresSubscription(StoreSubscriptionQuery query)
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

                var requestUrl = $"/api/v2/classifiedbo/stores-get-subscriptions?{queryString}";

                var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

                return await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAllStoresSubscription");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public Task<HttpResponseMessage> GetStoresById(string vertical, string adId)
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseMessage> UpdateStoresSubscription(int orderId, string status)
        {
            throw new NotImplementedException();
        }
    }
}
