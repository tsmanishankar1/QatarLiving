using Nextended.Core.Extensions;
using QLN.Web.Shared.Contracts;
using QLN.Web.Shared.Model;
using QLN.Web.Shared.Models;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using static QLN.Web.Shared.Models.ClassifiedsDashboardModel;

namespace QLN.Web.Shared.Services
{
    public class CommunityService : ServiceBase<CommunityService>, ICommunityService
    {
        private readonly HttpClient _httpClient;

        public CommunityService(HttpClient httpClient) : base(httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<(List<PostListDto> Posts, int TotalCount)> GetPostsAsync(int? forumId, string? order, int page, int pageSize)
        {

            var requestUri = $"/api/content/community?page={page}&page_size={pageSize}";

            if(forumId != null)
            {
                requestUri += $"&forum_id={forumId}";
            }

            if(!string.IsNullOrEmpty(order))
            {
                requestUri += $"&order={order}";
            }


            try
            {

                var response = await _httpClient.GetFromJsonAsync<PostListResponse>(requestUri);
                return (response.items, response.total);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API Error: {ex.Message}");
                return (null, 0);
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
        public async Task<bool> PostCommentAsync(CommentPostRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/content/comment/save", request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error posting comment: {ex.Message}");
                return false;
            }
        }
        public async Task<PaginatedCommentResponse> GetCommentsByPostIdAsync(int nid, int page, int pageSize)
        {
            try
            {
                var url = $"api/content/comments/{nid}?page={page}&page_size={pageSize}";
                var response = await _httpClient.GetFromJsonAsync<PaginatedCommentResponse>(url);
                return response ?? new PaginatedCommentResponse();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API Error: {ex.Message}");
                return new PaginatedCommentResponse();
            }
        }

        public class CategoryResponse
        {
            [JsonPropertyName("forum_categories")]
            public List<ForumCategory> Forum_Categories { get; set; }
        }

        public async Task<(List<CommunityPostModel> Posts, int TotalCount)> GetCommunityPostList(int? categoryId, string? search, int? page, int? pageSize, string? sortDirection)
        {
            try
            {
                var url = $"api/v2/community/getAllPosts?categoryId={categoryId}&search={search}&page={page}&pageSize={pageSize}&sortDirection={sortDirection}";

                var result = await _httpClient.GetFromJsonAsync<CommunityPostListResponse>(url);

                return (result?.Items ?? new List<CommunityPostModel>(), result?.Total ?? 0);

            }
            catch (Exception)
            {
                return (new List<CommunityPostModel>(), 0);
            }
        }



        public async Task<CommunityPostModel> GetCommunityPostDetail(string slug)
        {
            try
            {
                var url = $"api/v2/community/getBySlug/{slug}";
                var response = await _httpClient.GetFromJsonAsync<CommunityPostModel>(url);
                
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API Error: {ex.Message}");
                return null;
            }
        }

        public async Task<List<CommunityCategoryModel>> GetCommunityCategoriesAsync()
        {
            try
            {
                var result = await _httpClient.GetFromJsonAsync<CommunityCategoryResponse>("api/v2/community/getAllForumCategories");
                return result?.ForumCategories ?? new List<CommunityCategoryModel>();
            }
            catch
            {
                return new List<CommunityCategoryModel>();
            }
        }
        public async Task<bool> CreateCommunityPostAsync(CreateCommunityPostDto dto)
        {
            try
            {
                var url = "api/v2/community/createPost";

                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(dto, options);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = content
                };
              

                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch(Exception ex)
            {
                
                return false;
            }
        }
        public async Task<bool> PostCommentAsyncV2(CommentPostRequestDto dto)
        {
            try
            {
                var url = "api/v2/community/addCommentByCategoryId";

                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(dto, options);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = content
                };


                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {

                return false;
            }
        }

        public async Task<PaginatedCommentResponseV2> GetCommentsByPostIdAsyncV2(string postId, int page, int pageSize)
        {
            try
            {
                var url = $"api/v2/community/comments/byPost/{postId}?page={page}&perPage={pageSize}";

                var request = new HttpRequestMessage(HttpMethod.Get, url);

                var response = await _httpClient.SendAsync(request);

                var result = await response.Content.ReadFromJsonAsync<PaginatedCommentResponseV2>();
                return result ?? new PaginatedCommentResponseV2();
            
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API Exception: {ex.Message}");
                return new PaginatedCommentResponseV2();
            }
        }


        public async Task<bool> ReportCommunityPostAsync(string postId)
        {
            try
            {
                var url = "/api/v2/report/createcommunitypost";

                var body = new
                {
                    postId = postId
                };

                var json = JsonSerializer.Serialize(body, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = content
                };

                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reporting post: {ex.Message}");
                return false;
            }
        }
        public async Task<bool> ReportCommentAsync(string postId, string commentId)
        {
            try
            {
                var url = "/api/v2/report/createcommunitycomments";

                var payload = new
                {
                    postId = postId,
                    commentId = commentId
                };

                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = content
                };

                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reporting comment: {ex.Message}");
                return false;
            }
        }
        public async Task<bool> LikeCommunityPostAsync(string postId)
        {
            try
            {
                var url = "/api/v2/community/likePostByCategoryId";

                var body = new
                {
                    communityPostId = postId
                };

                var json = JsonSerializer.Serialize(body, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = content
                };

                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error liking post: {ex.Message}");
                return false;
            }
        }
        public async Task<bool> LikeCommunityCommentAsync(string postId,string commentId)
        {
            try
            {
                var url = "api/v2/community/likeCommentByUserId/";

                var body = new
                {
                    communityPostId = postId
                };

                var json = JsonSerializer.Serialize(body, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = content
                };

                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error liking post: {ex.Message}");
                return false;
            }
        }

    }
}