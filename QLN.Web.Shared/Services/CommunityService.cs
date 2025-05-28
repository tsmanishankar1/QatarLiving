using QLN.Web.Shared.Contracts;
using QLN.Web.Shared.Model;
using System.Net.Http.Json;

namespace QLN.Web.Shared.Services
{
    public class CommunityService : ICommunityService
    {
        private readonly HttpClient _httpClient;

        public CommunityService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<PostModel>> GetAllAsync()
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<PostModel>>("api/community/posts");
            return response ?? new List<PostModel>();
        }
    }
}
