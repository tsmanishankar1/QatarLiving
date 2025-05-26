using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.BannerService;

namespace QLN.Backend.API.Service
{
    public class ExternalContentService(HttpClient httpClient) : IContentService
    {
        const string LandingPath = "/qlnapi/landing/qln_contents_daily";
        public Task<ContentItem?> GetContentByIdAsync(string id)
        {

            throw new NotImplementedException();
        }

        public Task<ContentEvent?> GetEventByIdAsync(string id)
        {
            throw new NotImplementedException();
        }

        public async Task<ContentLandingPageResponse?> GetLandingPageAsync()
        {
            return await httpClient.GetFromJsonAsync<ContentLandingPageResponse>(LandingPath);
        }
    }
}
