using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.BannerService;

namespace QLN.Backend.API.Service
{
    public class ExternalContentService(HttpClient httpClient) : IContentService
    {
        const string LandingPath = "/qlnapi/landing/qln_contents_daily";
        const string GetEventBySlugPath = "/qlnapi/node?slug=";
        const string GetPostBySlugPath = "/qlnapi/node?slug=";

        public async Task<ContentLandingPageResponse?> GetLandingPageAsync()
        {
            return await httpClient.GetFromJsonAsync<ContentLandingPageResponse>(LandingPath);
        }

        public async Task<ContentPost?> GetPostBySlugAsync(string slug)
        {
            var results = await httpClient.GetFromJsonAsync<ContentPost>($"{GetPostBySlugPath}{slug}");

            if (results?.NodeType == "post")
            {
                return results;
            }

            return null;

        }

        public async Task<ContentEvent?> GetEventBySlugAsync(string slug)
        {
            var results = await httpClient.GetFromJsonAsync<ContentEvent>($"{GetEventBySlugPath}{slug}");

            if(results?.NodeType == "event")
            {
                return results;
            }

            return null;

        }

        
    }
}
