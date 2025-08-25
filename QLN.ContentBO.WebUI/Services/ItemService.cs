using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Services.Base;
using System.Net;
using System.Text;
using System.Text.Json;

namespace QLN.ContentBO.WebUI.Services
{
    public class ItemService : ServiceBase<ItemService>, IItemService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        public ItemService(HttpClient httpClient, ILogger<ItemService> Logger)
            : base(httpClient, Logger)
        {
            _httpClient = httpClient;
            _logger = Logger;
        }

        public async Task<HttpResponseMessage?> BulkItemsActionAsync(List<long?> adIds, int action, string? reason = null, string? comments = null)
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

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var requestUrl = "/api/v2/classifiedbo/bulk-items-action";
                return await _httpClient.PostAsync(requestUrl, content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BulkItemsActionAsync");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage?> GetAllItemsListing(ItemsRequest itemsRequest)
        {
            try
            {
                var requestUrl = "/api/v2/classifiedbo/getall-items";
                var content = new StringContent(JsonSerializer.Serialize(itemsRequest), Encoding.UTF8, "application/json");
                return await _httpClient.PostAsync(requestUrl, content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAllItemsListings");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage?> GetItemsTransactionListing(ItemTransactionRequest itemTransactionRequest)
        {
            try
            {
                var requestUrl = "/api/v2/classifiedbo/items/transactions";
                var json = JsonSerializer.Serialize(itemTransactionRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
                {
                    Content = content
                };
                var response = await _httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetItemsTransactionListing");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage?> CreateAd(object payload)
        {
            try
            {
                var endpoint = $"/api/v2/classifiedbo/items/admin/post-by-id";

                var serializeOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                };
                var jsonPayload = JsonSerializer.Serialize(payload, serializeOptions);
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
                };

                var response = await _httpClient.SendAsync(request);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("PostAdAsync " + ex);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
    }
}
