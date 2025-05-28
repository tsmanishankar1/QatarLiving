using Microsoft.Extensions.Options;
using QLN.Web.Shared.Contracts;
using QLN.Web.Shared.MockServices;
using QLN.Web.Shared.Model;
using System.Net.Http.Json;

namespace QLN.Web.Shared.Services
{
    public class CommunityService : ICommunityService
    {

        private readonly HttpClient _httpClient;
        private readonly ApiSettings _apiSettings;
        private readonly string _baseUrl;

        public CommunityService(HttpClient httpClient, IOptions<ApiSettings> options)
        {
            _httpClient = httpClient;
            //_apiSettings = options.Value;
            _baseUrl = "https://qlc-bo-dev.qatarliving.com/";


        }



        public async Task<List<PostListDto>> GetPostsAsync(int forumId, string order, int page, int pageSize)
        {
            try
            {
                var url = $"{_baseUrl}/api/content/community?" +
                          $"forum_id={forumId}&" +
                          $"order={order}&" +
                          $"page={page}&" +
                          $"page_size={pageSize}";

                Console.WriteLine($"[DEBUG] API Request URL: {url}");

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