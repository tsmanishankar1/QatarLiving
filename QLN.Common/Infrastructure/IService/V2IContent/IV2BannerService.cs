using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Subscriptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.V2IContent
{
    public interface IV2BannerService
    {
        Task<string> CreateBannerTypeAsync(V2BannerTypeDto dto, CancellationToken cancellationToken = default);
        Task<string> CreateBannerLocationAsync(V2BannerLocationDto dto, CancellationToken cancellationToken = default);
        Task<List<V2BannerLocationDto>> GetAllBannerLocationsAsync(CancellationToken cancellationToken = default);
        Task<string> CreateBannerPageLocationAsync(V2BannerPageLocationDto dto, CancellationToken cancellationToken = default);
        Task<List<BannerTypeDetailsDto>> GetBannerTypesByVerticalAsync(Vertical vertical, CancellationToken cancellationToken = default);
        Task<List<BannerTypeDetailsDto>> GetBannerTypesBySubVerticalAsync(SubVertical subVertical, CancellationToken cancellationToken = default);
        Task<List<BannerTypeDetailsDto>> GetBannerTypesByPageIdAsync(Guid pageId, CancellationToken cancellationToken = default);
        Task<string> CreateBannerAsync(string uid, V2BannerDto dto, CancellationToken cancellationToken = default);
    }
       
}
