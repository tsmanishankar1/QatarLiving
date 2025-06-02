using QLN.Common.Infrastructure.DTO_s;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.IBannerService
{
    public interface IBannerService
    {
        //Task<BannerResponse?> GetBannersAsync(CancellationToken cancellationToken);
        Task<dynamic?> GetBannersAsync(CancellationToken cancellationToken);
    }
}
