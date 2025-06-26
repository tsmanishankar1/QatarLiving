using QLN.Common.DTO_s;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService
{
    public interface IServicesService
    {
        Task<ServiceDashboardWithAdsDto> GetDashboardAndAds(string userId, CancellationToken cancellationToken = default);
    }
}
