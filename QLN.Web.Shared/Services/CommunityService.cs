using QLN.Web.Shared.Contracts;
using QLN.Web.Shared.Model;
using QLN.Web.Shared.Models;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

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
        public async Task<PostDetailsDto> GetPostBySlugAsync(string slug)
        {
            try
            {
                var url = $"api/content/post/{slug}";
                var response = await _httpClient.GetFromJsonAsync<PostDetailsDto>(url);
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API Error: {ex.Message}");
                return null;
            }
        }
        public async Task<List<MorePostItem>> GetMorePostsAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<MorePostsResponse>("api/content/qln_community_post/landing");
                return response?.qln_community_post?.qln_community_post_more_posts?.items ?? new List<MorePostItem>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API Error: {ex.Message}");
                return new List<MorePostItem>();
            }
        }
        public async Task<List<SelectOption>> GetForumCategoriesAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<CategoryResponse>("api/content/categories");
                var forumCategories = response?.Forum_Categories ?? new List<ForumCategory>();

                return forumCategories.Select(cat => new SelectOption
                {
                    Id = cat.Id,
                    Label = cat.Name
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API Error: {ex.Message}");
                return new List<SelectOption>();
            }
        }
        public class CategoryResponse
        {
            [JsonPropertyName("forum_categories")]
            public List<ForumCategory> Forum_Categories { get; set; }
        }
        

    }
}