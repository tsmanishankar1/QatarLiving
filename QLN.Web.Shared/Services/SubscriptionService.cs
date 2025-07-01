using Microsoft.Extensions.Logging;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Pages.Subscription;
using QLN.Web.Shared.Services.Interface;
using System.Net.Http.Headers;
using System.Net.Http.Json;


namespace QLN.Web.Shared.Services
{
    public class SubscriptionService : ServiceBase<SubscriptionService>, ISubscriptionService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<SubscriptionService> _logger;


        public SubscriptionService(HttpClient httpClient, ILogger<SubscriptionService> logger) : base(httpClient)
        {
            _httpClient = httpClient;
            _logger = logger;

        }


        public async Task<bool> AddSubscriptionAsync(SubscriptionModel model)
        {
            var payload = new
            {
                model.SubscriptionName,
                model.Price,
                model.Currency,
                model.Duration,
                model.VerticalType,
                model.SubCategory,
                model.Description
            };
            var response = await _httpClient.PostAsJsonAsync("api/subscription/add", payload);
            return response.IsSuccessStatusCode;
         
        }

        public async Task<SubscriptionResponse?> GetSubscriptionAsync(int verticalId, int categoryId)
        {
            try
            {
                var response = await _httpClient.GetAsync(
                           $"api/subscriptions/getsubscription?verticalTypeId={verticalId}&categoryId={categoryId}");

                if (!response.IsSuccessStatusCode)
                    return null;

                var result = await response.Content.ReadFromJsonAsync<SubscriptionResponse>();
                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"HTTP request error: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error: {ex.Message}");
                return null;
            }
        }


        public async Task<bool> UpdateSubscriptionAsync(SubscriptionModel model)
        {
            var payload = new
            {
                model.SubscriptionName,
                model.Price,
                model.Currency,
                model.Duration,
                model.VerticalType,
                model.SubCategory,
                model.Description
            };
            var response = await _httpClient.PatchAsJsonAsync("api/subscription/update", payload);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteSubscriptionAsync(Guid id)
        {
            var response = await _httpClient.DeleteAsync($"api/subscription/delete/{id}");
            return response.IsSuccessStatusCode;
        }
        public async Task<bool> PurchaseSubscription(object payload)
        {

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "api/payments/subscribe")
                {
                    Content = JsonContent.Create(payload)
                };

               

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                  
                }
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"Error in PurchaseSubscription: {ex.Message}");
                return false;
            }

        }
        public async Task<List<PayToPublishPlan>?> GetPayToPublishPlansAsync(int verticalId, int categoryId)
        {
            try
            {
                var url = $"api/paytopublish/getpaytopublish?verticalTypeId={verticalId}&categoryId={categoryId}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return null;

                var result = await response.Content.ReadFromJsonAsync<List<PayToPublishPlan>>();
                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"[PayToPublish] HTTP error: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[PayToPublish] Unexpected error: {ex.Message}");
                return null;
            }
        }
        public async Task<List<PayToPublishPlan>?> GetPayToFeatureAsync(int verticalId, int categoryId)
        {
            try
            {
                var url = $"api/paytofeature/getpaytofeature?verticalTypeId={verticalId}&categoryId={categoryId}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return null;

                var result = await response.Content.ReadFromJsonAsync<List<PayToPublishPlan>>();
                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"[PayToPublish] HTTP error: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[PayToPublish] Unexpected error: {ex.Message}");
                return null;
            }
        }

    }

}
