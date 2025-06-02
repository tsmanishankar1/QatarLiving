using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.IBannerService;
using System.Net.Http;

namespace QLN.Backend.API.Service.BannerService
{
    public class ExternalBannerService(HttpClient httpClient) : IBannerService
    {
        //public async Task<BannerResponse?> GetBannersAsync(CancellationToken cancellationToken)
        //{
        //    return await httpClient.GetFromJsonAsync<BannerResponse>(DrupalContentConstants.BannerPath, cancellationToken);
        //}

        public async Task<dynamic?> GetBannersAsync(CancellationToken cancellationToken)
        {
            return await httpClient.GetFromJsonAsync<dynamic>(DrupalContentConstants.BannerPath, cancellationToken);
        }
    }
}
