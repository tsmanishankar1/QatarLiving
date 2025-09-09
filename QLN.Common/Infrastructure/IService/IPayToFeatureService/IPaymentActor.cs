using Dapr.Actors;
using QLN.Common.DTO_s;
using QLN.Common.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.IPayToFeatureService
{
    public interface IPaymentActor : IActor
    {
        Task<bool> SetDataAsync(PayToFeaturePaymentDto data, CancellationToken cancellationToken = default);
        Task<bool> FastSetDataAsync(PayToFeaturePaymentDto data, CancellationToken cancellationToken = default);
        Task<PayToFeaturePaymentDto?> GetDataAsync(CancellationToken cancellationToken = default);
        Task<bool> AddPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken = default);
        Task<List<Guid>> GetAllPaymentIdsAsync(CancellationToken cancellationToken = default);
        Task StorePaymentDetailsAsync(UserP2FPaymentDetailsResponseDto details, CancellationToken cancellationToken = default);
        Task<List<UserP2FPaymentDetailsResponseDto>> GetAllPaymentDetailsAsync(CancellationToken cancellationToken = default);
        Task<bool> DeleteDataAsync(CancellationToken cancellationToken = default);
        Task<bool> SyncStateKeysAsync(CancellationToken cancellationToken = default);
        Task CheckPaytoFeatureExpiryAsync();
        Task<bool> TriggerExpiryCheckAsync();
        Task<(bool IsActive, DateTime? EndDate, int? DaysRemaining)> GetSubscriptionStatusAsync();
        Task<bool> RescheduleExpiryChecksAsync();
    }
}
