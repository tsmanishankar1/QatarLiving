using Microsoft.Extensions.Configuration;
using QLN.Common.DTO_s.InstagramDto;
using QLN.Common.Infrastructure.IService.IInstagramPost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.Service.InstaPost
{
    public class InstagramPostService : IInstaService
    {
        private readonly HttpClient _httpClient;
        private readonly string _accessToken;
        private readonly string _baseUrl;

        public InstagramPostService(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClient = httpClientFactory.CreateClient();
            _accessToken = config["Instagram:AccessToken"]
                           ?? throw new ArgumentNullException("Instagram:AccessToken not found in appsettings.json");
            _baseUrl = config["Instagram:BaseUrl"]
                       ?? "https://graph.instagram.com/v21.0"; // fallback in case not found
        }

        public async Task<List<InstagramPost>> GetLatestPostsAsync(int count = 10, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/me/media" +
                      $"?fields=id,media_type,media_url,caption,timestamp,media_product_type" +
                      $"&limit={count}" +
                      $"&access_token={_accessToken}";

            var response = await _httpClient.GetFromJsonAsync<InstagramResponse>(url, cancellationToken);

            if (response?.Data == null)
                return new List<InstagramPost>();

            return response.Data
                .Where(p => !string.Equals(p.MediaProductType, "VIDEO", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

    }
}
