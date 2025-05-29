using QLN.Web.Shared.Contracts;
using QLN.Web.Shared.Model;
using System.Net.Http.Json;

namespace QLN.Web.Shared.Services
{
    public class CommunityService : ServiceBase<CommunityService>, ICommunityService
    {
        private readonly HttpClient _httpClient;

        public CommunityService(HttpClient httpClient): base(httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<PostListDto>> GetPostsAsync(int forumId, string order, int page, int pageSize)
        {

            try
            {
                var url = $"api/content/community?" +
                          $"forum_id={forumId}&" +
                          $"order={order}&" +
                          $"page={page}&" +
                          $"page_size={pageSize}";


                var response = await _httpClient.GetFromJsonAsync<List<PostListDto>>(url);
                return response ?? new List<PostListDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API Error: {ex.Message}");
                return new List<PostListDto>();
            }
        }

    }
}