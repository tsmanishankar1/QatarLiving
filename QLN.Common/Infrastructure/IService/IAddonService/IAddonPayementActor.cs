using Dapr.Actors;
using QLN.Common.DTO_s;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static QLN.Common.DTO_s.AddonDto;

namespace QLN.Common.Infrastructure.IService.IAddonService
{
    public interface IAddonPaymentActor : IActor
    {
        Task<bool> SetDataAsync(AddonPaymentDto data, CancellationToken cancellationToken = default);
        Task<bool> FastSetDataAsync(AddonPaymentDto data, CancellationToken cancellationToken = default);
        Task<AddonPaymentDto?> GetDataAsync(CancellationToken cancellationToken = default);
        Task CheckAddonExpiryAsync();
        Task<bool> TriggerExpiryCheckAsync();
        Task<(bool IsActive, DateTime? EndDate, int? DaysRemaining)> GetSubscriptionStatusAsync();
        Task<bool> RescheduleExpiryChecksAsync();
    }
}
