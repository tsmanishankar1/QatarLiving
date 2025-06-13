using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.DTO_s;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.V2IContent
{
 

    public interface IV2contentBannerService
    {
        Task<BannerResponse> SaveBannerAsync(BannerCreateRequest dto, string userId, CancellationToken cancellationToken = default);
        Task<List<BannerItem>> GetBannersByCategoryAsync(string category, CancellationToken cancellationToken = default);
    }
}
