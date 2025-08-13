using QLN.Common.DTO_s.Implio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.IImplio
{
    public interface IImplioService
    {
        Task<ImplioModerationResponse?> CreateModerationRequest(List<ImplioModerationRequest> requests, CancellationToken cancellationToken = default);
        Task<ImplioModerationResponse?> UpdateModerationRequest(List<ImplioModerationRequest> requests, CancellationToken cancellationToken = default);
        Task<ImplioGetResponse?> GetModerationRequests(int timestamp, string taskIds, bool noAdContent = true, CancellationToken cancellationToken = default);
        Task<bool> DeleteModerationRequests(string userId, string taskIds, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
        Task<bool> ImplioHealthCheck(CancellationToken cancellationToken = default);
    }
}
