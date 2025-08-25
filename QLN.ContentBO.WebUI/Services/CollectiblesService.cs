using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Services.Base;
using System.Net;
using System.Text;
using System.Text.Json;

namespace QLN.ContentBO.WebUI.Services
{
    public class CollectiblesService : ServiceBase<CollectiblesService>, ICollectiblesService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        public CollectiblesService(HttpClient httpClient, ILogger<CollectiblesService> Logger)
            : base(httpClient, Logger)
        {
            _httpClient = httpClient;
            _logger = Logger;
        }

        public async Task<HttpResponseMessage?> BulkActionAsync(
    List<long?> adIds,
    int action,
    string? reason = null,
    string? comments = null)
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
                var jsonPayload = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    WriteIndented = true 
                });
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var requestUrl = "/api/v2/classifiedbo/bulk-collectibles-action";
                return await _httpClient.PostAsync(requestUrl, content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BulkActionAsync");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }


        public async Task<HttpResponseMessage?> GetAllListing(ItemsRequest itemsRequest)
        {
            try
            {
                var requestUrl = "/api/v2/classifiedbo/getall-collectibles";
                var content = new StringContent(JsonSerializer.Serialize(itemsRequest), Encoding.UTF8, "application/json");
                return await _httpClient.PostAsync(requestUrl, content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAllListing");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage?> GetTransactionListing(ItemTransactionRequest itemTransactionRequest)
        {
            try
            {
                var jsonPayload = JsonSerializer.Serialize(itemTransactionRequest);
                var request = new HttpRequestMessage(HttpMethod.Post, "/api/v2/classifiedbo/items/transactions")
                {
                    Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
                };
                return await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetTransactionListing");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

    }
}
