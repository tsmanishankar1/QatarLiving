using QLN.Common.DTO_s;
using QLN.Common.DTO_s;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.ISearchService
{
    public interface IAnalyticsService
    {
        Task<AnalyticsIndex?> GetAsync(string section, string entityId);
        Task UpsertAsync(AnalyticsEventRequest request);
        Task<ApiResponse<object>> GetAnalyticsAsync(AnalyticsRequestDto request, string userId, CancellationToken cancellationToken = default);
    }
}
