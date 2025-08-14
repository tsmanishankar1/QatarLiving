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
                var queryParams = new Dictionary<string, string?>
                {
                    { "subVertical", itemTransactionRequest.SubVertical.ToString() },
                    { "status", itemTransactionRequest.Status },
                    { "createdDate", itemTransactionRequest.DateCreated },
                    { "publishedDate", itemTransactionRequest.DatePublished },
                    { "dateStart", itemTransactionRequest.DateStart },
                    { "dateEnd", itemTransactionRequest.DateEnd },
                    { "page", itemTransactionRequest.PageNumber.ToString() },
                    { "pageSize", itemTransactionRequest.PageSize.ToString() },
                    { "search", itemTransactionRequest.SearchText },
                    { "productType", itemTransactionRequest.ProductType },
                    { "paymentMethod", itemTransactionRequest.PaymentMethod },
                    { "sortBy", itemTransactionRequest.SortBy },
                    { "sortOrder", itemTransactionRequest.SortOrder }
                };

                var queryString = string.Join("&",
                    queryParams
                        .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Value))
                        .Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value!)}"));

                var requestUrl = $"/api/v2/classifiedbo/items/transactions?{queryString}";

                var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

                return await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetItemsTransactionListing");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
    }
}
