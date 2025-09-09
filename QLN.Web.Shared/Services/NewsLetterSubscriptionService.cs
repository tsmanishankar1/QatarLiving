using QLN.Web.Shared.Contracts;
using QLN.Web.Shared.Model;
using System.Net.Http.Json;

namespace QLN.Web.Shared.Services
{
    public class NewsLetterSubscriptionService : ServiceBase<NewsLetterSubscriptionService>, INewsLetterSubscription
    {
        private readonly HttpClient _httpClient;

        public NewsLetterSubscriptionService(HttpClient httpClient) : base(httpClient)
        {
            _httpClient = httpClient;
        }


        public async Task<bool> SubscribeAsync(NewsLetterSubscriptionModel model)
        {
            try
            {

                var response = await _httpClient.PostAsJsonAsync("subscribe/post?u=3ab0436d22c64716e67a03f64&id=94198fac96", new { EMAIL = model.Email });
                var responseBody = await response.Content.ReadAsStringAsync();
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Newsletter] Error during subscription: {ex.Message}");
                return false;
            }
        }

    }
}