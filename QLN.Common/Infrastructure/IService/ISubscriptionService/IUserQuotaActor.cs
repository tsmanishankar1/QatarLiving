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
       
           
           

       
        Task UpsertQuotaAsync(GenericUserQuotaDto newQuota, CancellationToken cancellationToken = default);
        Task<UserQuotaCollection?> GetAllQuotasAsync(CancellationToken cancellationToken = default);
        Task<List<GenericUserQuotaDto>> GetActiveQuotasAsync(CancellationToken cancellationToken = default);
        Task<bool> UpdateQuotaAsync(Guid transactionId, GenericUserQuotaDto updatedQuota, CancellationToken cancellationToken = default);
        Task<bool> DeleteQuotaAsync(Guid transactionId, CancellationToken cancellationToken = default);
    }
}
