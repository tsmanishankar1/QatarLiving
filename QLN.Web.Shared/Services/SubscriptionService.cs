using QLN.Web.Shared.Models;
using QLN.Web.Shared.Services.Interfaces;


namespace QLN.Web.Shared.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly HttpClient _http;
        private readonly ApiService _api;

        public SubscriptionService(HttpClient http, ApiService api)
        {
            _http = http;
            _api = api;
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

            var result = await _api.PostAsync<object, object>("api/subscription/add", payload);
            return result is not null;
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

            var result = await _api.PatchAsync<object, object>("api/subscription/edit", payload);
            return result is not null;
        }

        public async Task<bool> DeleteSubscriptionAsync(Guid id)
        {
            var result = await _api.DeleteAsync($"api/subscription/delete/{id}");
            return result;
        }

    }

}
