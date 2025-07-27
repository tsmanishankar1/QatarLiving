using QLN.Common.DTO_s;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.ISubscriptionService
{
    public interface IUserQuotaService
    {
        // Write operation
        Task UpsertQuotaAsync(string userId, GenericUserQuotaDto newQuota, CancellationToken cancellationToken = default);

        // Read operations
        Task<UserQuotaCollection?> GetUserQuotasAsync(string userId, CancellationToken cancellationToken = default);
        Task<List<GenericUserQuotaDto>> GetActiveUserQuotasAsync(string userId, CancellationToken cancellationToken = default);

        // Update/Delete operations
        Task<bool> UpdateUserQuotaAsync(string userId, Guid transactionId, GenericUserQuotaDto updatedQuota, CancellationToken cancellationToken = default);
        Task<bool> DeleteUserQuotaAsync(string userId, Guid transactionId, CancellationToken cancellationToken = default);
    }
}
