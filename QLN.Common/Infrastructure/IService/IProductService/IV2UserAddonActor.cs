using Dapr.Actors;
using QLN.Common.DTO_s.Subscription;
using QLN.Common.Infrastructure.Subscriptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.IProductService
{
    /// <summary>
    /// V2 User Addon Actor Interface - Handles all addon-related operations
    /// Actors manage their own persistence, state, and event publishing
    /// </summary>
    public interface IV2UserAddonActor : IActor
    {
        /// <summary>
        /// Creates a new addon with complete database persistence and event publishing
        /// Handles: Product validation, DB transaction, actor state creation, event publishing
        /// </summary>
        /// <param name="request">Addon purchase request with all required data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if addon created successfully</returns>
        Task<bool> CreateAddonAsync(V2UserAddonPurchaseRequestDto request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets addon data with fast write operation to actor state
        /// Used for internal state management and updates
        /// Includes throttling and error handling for stability
        /// </summary>
        /// <param name="data">Complete addon data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if data saved successfully</returns>
        Task<bool> FastSetDataAsync(V2UserAddonDto data, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets addon data (alias for FastSetDataAsync for backward compatibility)
        /// </summary>
        /// <param name="data">Complete addon data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if data saved successfully</returns>
        Task<bool> SetDataAsync(V2UserAddonDto data, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets addon data from actor state
        /// Includes memory caching and throttling for performance
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Current addon data or null if not found</returns>
        Task<V2UserAddonDto?> GetDataAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates if addon has enough quota for the requested usage
        /// Checks active status, expiry, and quota availability
        /// </summary>
        /// <param name="quotaType">Type of quota to validate (e.g., "ads_budget", "promote_budget")</param>
        /// <param name="requestedAmount">Amount requested to use</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if quota is available and addon is active</returns>
        Task<bool> ValidateUsageAsync(string quotaType, int requestedAmount, CancellationToken cancellationToken = default);

        /// <summary>
        /// Records usage against addon quota with database persistence
        /// Updates both actor state and database quota in a transaction
        /// </summary>
        /// <param name="quotaType">Type of quota to record against</param>
        /// <param name="amount">Amount to record</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if usage recorded successfully</returns>
        Task<bool> RecordUsageAsync(string quotaType, int amount, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if addon is currently active
        /// Validates both status and end date
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if addon is active and not expired</returns>
        Task<bool> IsActiveAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks addon as expired with database persistence and event publishing
        /// Updates status, persists to DB, and publishes expiration event
        /// Called by scheduler or expiry processing
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if expiration marking successful</returns>
        Task<bool> MarkAsExpiredAsync(CancellationToken cancellationToken = default);
        Task<bool> UpdateStatusAsync(SubscriptionStatus newStatus, CancellationToken cancellationToken = default);
        Task<bool> UpdateEndDateAsync(DateTime newEndDate, CancellationToken cancellationToken = default);
        Task<bool> ExtendAddonAsync(TimeSpan additionalDuration, CancellationToken cancellationToken = default);
        Task<bool> RefillQuotaAsync(string quotaType, decimal amount, CancellationToken cancellationToken = default);
        Task<bool> CancelAddonAsync(string userId, CancellationToken cancellationToken = default);
        Task<bool> AdminCancelAddonAsync(CancellationToken cancellationToken = default);
    }
}
