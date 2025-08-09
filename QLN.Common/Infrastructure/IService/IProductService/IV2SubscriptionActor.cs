using Dapr.Actors;
using Dapr.Actors.Runtime;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QLN.Common.DTO_s.Subscription;
using QLN.Common.DTOs;
using QLN.Common.Infrastructure.Subscriptions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.IProductService
{
    public interface IV2SubscriptionActor : IActor
    {
        /// <summary>
        /// Sets subscription data with fast write operation
        /// </summary>
        Task<bool> FastSetDataAsync(V2SubscriptionDto data, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets subscription data (alias for FastSetDataAsync)
        /// </summary>
        Task<bool> SetDataAsync(V2SubscriptionDto data, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets subscription data from actor state
        /// </summary>
        Task<V2SubscriptionDto?> GetDataAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates if subscription has enough quota for the requested usage
        /// </summary>
        Task<bool> ValidateUsageAsync(string quotaType, decimal requestedAmount, CancellationToken cancellationToken = default);

        /// <summary>
        /// Records usage against subscription quota
        /// </summary>
        Task<bool> RecordUsageAsync(string quotaType, decimal amount, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if subscription is currently active
        /// </summary>
        Task<bool> IsActiveAsync(CancellationToken cancellationToken = default);
    }
}
