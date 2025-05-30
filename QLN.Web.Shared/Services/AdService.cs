using QLN.Web.Shared.Contracts;
using QLN.Web.Shared.Model;
using System.Net.Http.Json;

namespace QLN.Web.Shared.Services
{
    public class AdService : IAdService
    {
        private readonly HttpClient _httpClient;

        public AdService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<AdModel>> GetAdDetail()
        {
            return await _httpClient.GetFromJsonAsync<IEnumerable<AdModel>>("api/get/ads");
        }
    }
}
