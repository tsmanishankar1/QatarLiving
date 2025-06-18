using QLN.Web.Shared.Models;
using QLN.Web.Shared.Services.Interface;
using System.Net;
using System.Text.Json;
using static QLN.Web.Shared.Pages.Subscription.SubscriptionDetails;

namespace QLN.Web.Shared.Services
{
    public class ClassfiedDashboardService : ServiceBase<ClassfiedDashboardService>, IClassifiedDashboardService
    {
        private readonly HttpClient _httpClient;


        public ClassfiedDashboardService(HttpClient httpClient) : base(httpClient)
        {
            _httpClient = httpClient;
        }
        /// <summary>
        /// Gets Classified Items Dashboard data.
        /// </summary>
        /// <returns>HttpResponseMessage</returns>
        public async Task<ItemDashboardResponse?> GetItemDashboard(string authToken)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "api/classified/itemsAd-dashboard");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ItemDashboardResponse>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return result;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetItemDashboard Exception: " + ex.Message);
                return null;
            }
        }

       

    }
}
