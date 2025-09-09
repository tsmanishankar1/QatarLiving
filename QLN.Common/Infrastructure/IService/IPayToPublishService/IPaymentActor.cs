using Dapr.Actors;
using QLN.Common.DTO_s;
using QLN.Common.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.IPayToPublishService
{
    public interface IPaymentActor : IActor
    {
        Task<bool> SetDataAsync(PaymentDto data, CancellationToken cancellationToken = default);
        Task<bool> FastSetDataAsync(PaymentDto data, CancellationToken cancellationToken = default);
        Task<PaymentDto?> GetDataAsync(CancellationToken cancellationToken = default);
        Task<bool> AddPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken = default);
        Task<List<Guid>> GetAllPaymentIdsAsync(CancellationToken cancellationToken = default);
        Task<bool> DeleteDataAsync(CancellationToken cancellationToken = default);
        Task StorePaymentDetailsAsync(UserP2PPaymentDetailsResponseDto details, CancellationToken cancellationToken = default);
        Task<List<UserP2PPaymentDetailsResponseDto>> GetAllPaymentDetailsAsync(CancellationToken cancellationToken = default);
        Task<bool> SyncStateKeysAsync(CancellationToken cancellationToken = default);
        Task<bool> TriggerExpiryCheckAsync();
        Task<(bool IsActive, DateTime? EndDate, int? DaysRemaining)> GetSubscriptionStatusAsync();
        Task<bool> RescheduleExpiryChecksAsync();
        Task CheckPaytopublishExpiryAsync();
    }
}
