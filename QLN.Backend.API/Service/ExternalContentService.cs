using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.BannerService;

namespace QLN.Backend.API.Service
{
    public class ExternalContentService(HttpClient httpClient) : IContentService
    {
        const string LandingPath = "/qlnapi/landing/qln_contents_daily";
        const string GetEventBySlugPath = "/qlnapi/node?slug=";
        const string GetPostBySlugPath = "/qlnapi/node?slug=";
        public async Task<ContentPost?> GetPostBySlugAsync(string slug)
        {
            return await httpClient.GetFromJsonAsync<ContentPost>($"{GetPostBySlugPath}{slug}");

        }

        public async Task<ContentEvent?> GetEventBySlugAsync(string slug)
        {
            return await httpClient.GetFromJsonAsync<ContentEvent>($"{GetEventBySlugPath}{slug}");

        }

        public async Task<ContentLandingPageResponse?> GetLandingPageAsync()
        {
            return await httpClient.GetFromJsonAsync<ContentLandingPageResponse>(LandingPath);
        }
    }
}
