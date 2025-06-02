using Microsoft.AspNetCore.Http;
using QLN.Web.Shared.Contracts;
using QLN.Web.Shared.Model;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace QLN.Web.Shared.Services
{

    public class PostInteractionService : IPostInteractionService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PostInteractionService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<bool> LikeOrUnlikeAsync(PostInteractionRequest request)
        {
            var token = _httpContextAccessor.HttpContext?.Request.Cookies["access_token"];
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await _httpClient.PostAsJsonAsync("api/posts/interact", request);
            return response.IsSuccessStatusCode;
        }

    }
}