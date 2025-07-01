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

      
        public SubscriptionService(HttpClient httpClient) : base(httpClient)
        {
            _httpClient = httpClient;
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
                Console.WriteLine($"HTTP request error: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
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
                    Console.WriteLine($"API Error Response: {errorContent}");
                    Console.WriteLine($"Status Code: {response.StatusCode}");
                }
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error in PurchaseSubscription: {ex.Message}");
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
                Console.WriteLine($"[PayToPublish] HTTP error: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PayToPublish] Unexpected error: {ex.Message}");
                return null;
            }
        }

    }

}
