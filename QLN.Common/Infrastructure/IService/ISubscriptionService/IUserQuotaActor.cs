using Dapr.Actors;
using QLN.Common.DTO_s;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.ISubscriptionService
{
    public interface IUserQuotaActor : IActor
    {
        // Add the missing UpsertQuotaAsync method
        Task UpsertQuotaAsync(GenericUserQuotaDto newQuota, CancellationToken cancellationToken = default);

        // Existing methods
        Task<UserQuotaCollection?> GetAllQuotasAsync(CancellationToken cancellationToken = default);
        Task<List<GenericUserQuotaDto>> GetActiveQuotasAsync(CancellationToken cancellationToken = default);
        Task<bool> UpdateQuotaAsync(Guid transactionId, GenericUserQuotaDto updatedQuota, CancellationToken cancellationToken = default);
        Task<bool> DeleteQuotaAsync(Guid transactionId, CancellationToken cancellationToken = default);
    }
}
