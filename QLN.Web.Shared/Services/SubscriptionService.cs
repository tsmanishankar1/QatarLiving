using QLN.Web.Shared.Models;
using QLN.Web.Shared.Pages.Subscription;
using QLN.Web.Shared.Services.Interfaces;
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

        public async Task<SubscriptionModel?> GetSubscriptionAsync(Guid id)
        {
            throw new NotImplementedException();
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
          
            var response = await _httpClient.PostAsJsonAsync("api/subscription/add", payload);
            return response.IsSuccessStatusCode;

        }
    }

}
