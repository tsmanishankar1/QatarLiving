using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.IBannerService;

namespace QLN.Backend.API.Service.BannerService
{
    public class ExternalBannerService(HttpClient httpClient) : IBannerService
    {
        //public async Task<BannerResponse?> GetBannersAsync(CancellationToken cancellationToken)
        //{
        //    return await httpClient.GetFromJsonAsync<BannerResponse>(DrupalContentConstants.BannerPath, cancellationToken);
        //}

        public async Task<BannerResponse?> GetBannersAsync(CancellationToken cancellationToken)
        {
            return await httpClient.GetFromJsonAsync<BannerResponse>(DrupalContentConstants.BannerPath, cancellationToken);
        }
    }
}
