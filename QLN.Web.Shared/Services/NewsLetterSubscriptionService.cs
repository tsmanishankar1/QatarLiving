using QLN.Web.Shared.Contracts;
using QLN.Web.Shared.Model;
using System.Net.Http.Json;

namespace QLN.Web.Shared.Services
{
    public class NewsLetterSubscriptionService : INewsLetterSubscription
    {
        private readonly HttpClient _httpClient;

        public NewsLetterSubscriptionService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<bool> SubscribeAsync(NewsLetterSubscriptionModel model)
        {
            var response = await _httpClient.PostAsJsonAsync("api/newsletter/subscribe", new { email = model.Email });

            return response.IsSuccessStatusCode;
        }

    }
}   