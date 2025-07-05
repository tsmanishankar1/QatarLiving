using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Services.Base;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace QLN.ContentBO.WebUI.Services
{
    public class CommunityService : ServiceBase<CommunityService>, ICommunityService
    {
        private readonly HttpClient _httpClient;

        public CommunityService(HttpClient httpClient, ILogger<CommunityService> Logger)
            : base(httpClient, Logger)
        {
            _httpClient = httpClient;
        }

        public async Task<HttpResponseMessage> GetAllCommunityPosts(string? categoryId, string? search, int page, int pageSize, string? sortDirection)
    {
        try
        {
            var query = new Dictionary<string, string?>
            {
                ["categoryId"] = categoryId,
                ["search"] = search,
                ["page"] = page.ToString(),
                ["pageSize"] = pageSize.ToString(),
                ["sortDirection"] = sortDirection
            };

            var queryString = string.Join("&", query
                .Where(kv => !string.IsNullOrEmpty(kv.Value))
                .Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value!)}"));

            var request = new HttpRequestMessage(HttpMethod.Get, $"api/v2/community/getAllPosts?{queryString}");
            return await _httpClient.SendAsync(request);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in GetAllCommunityPosts");
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        }
    }


        public async Task<HttpResponseMessage> DeleteCommunity(string id)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, $"api/v2/community/deletePost/{id}");
            return await _httpClient.SendAsync(request);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in DeleteCommunity");
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        }
    }


    }
}
