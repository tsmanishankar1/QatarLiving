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
    /// <summary>
    /// V2 Subscription Actor Interface - Handles all subscription-related operations
    /// Actors manage their own persistence, state, and event publishing
    /// </summary>
    public interface IV2SubscriptionActor : IActor
    {
        /// <summary>
        /// Creates a new subscription with complete database persistence and event publishing
        /// Handles: Product validation, DB transaction, actor state creation, event publishing
        /// </summary>
        /// <param name="request">Subscription purchase request with all required data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if subscription created successfully</returns>
        Task<bool> CreateSubscriptionAsync(V2SubscriptionPurchaseRequestDto request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets subscription data with fast write operation to actor state
        /// Used for internal state management and updates
        /// </summary>
        /// <param name="data">Complete subscription data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if data saved successfully</returns>
        Task<bool> FastSetDataAsync(V2SubscriptionDto data, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets subscription data (alias for FastSetDataAsync for backward compatibility)
        /// </summary>
        /// <param name="data">Complete subscription data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if data saved successfully</returns>
        Task<bool> SetDataAsync(V2SubscriptionDto data, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets subscription data from actor state with database sync
        /// Syncs with database if needed to handle admin changes
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Current subscription data or null if not found</returns>
        Task<V2SubscriptionDto?> GetDataAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates if subscription has enough quota for the requested usage
        /// Checks active status, expiry, and quota availability
        /// </summary>
        /// <param name="quotaType">Type of quota to validate (e.g., "ads_budget", "promote_budget")</param>
        /// <param name="requestedAmount">Amount requested to use</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if quota is available and subscription is active</returns>
        Task<bool> ValidateUsageAsync(string quotaType, decimal requestedAmount, CancellationToken cancellationToken = default);

        /// <summary>
        /// Records usage against subscription quota with database persistence
        /// Updates both actor state and database quota in a transaction
        /// </summary>
        /// <param name="quotaType">Type of quota to record against</param>
        /// <param name="amount">Amount to record</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if usage recorded successfully</returns>
        Task<bool> RecordUsageAsync(string quotaType, decimal amount, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if subscription is currently active
        /// Validates both status and end date
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if subscription is active and not expired</returns>
        Task<bool> IsActiveAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancels subscription with database persistence and event publishing
        /// Updates status, persists to DB, and publishes cancellation event
        /// </summary>
        /// <param name="userId">User ID to verify ownership</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if cancellation successful</returns>
        Task<bool> CancelSubscriptionAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Extends subscription end date with database persistence
        /// Updates both actor state and database in a transaction
        /// </summary>
        /// <param name="additionalDuration">Duration to add to subscription</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if extension successful</returns>
        Task<bool> ExtendSubscriptionAsync(TimeSpan additionalDuration, CancellationToken cancellationToken = default);

        /// <summary>
        /// Refills subscription quota with database persistence
        /// Adds quota to specified quota type in both actor and database
        /// </summary>
        /// <param name="quotaType">Type of quota to refill</param>
        /// <param name="amount">Amount to add</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if refill successful</returns>
        Task<bool> RefillQuotaAsync(string quotaType, decimal amount, CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks subscription as expired with database persistence and event publishing
        /// Updates status, persists to DB, and publishes expiration event
        /// Called by scheduler or expiry reminders
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if expiration marking successful</returns>
        Task<bool> MarkAsExpiredAsync(CancellationToken cancellationToken = default);
        Task<bool> UpdateStatusAsync(SubscriptionStatus newStatus, CancellationToken cancellationToken = default);
        Task<bool> UpdateEndDateAsync(DateTime newEndDate, CancellationToken cancellationToken = default);
        Task<bool> AdminCancelSubscriptionAsync(CancellationToken cancellationToken = default);
        Task<bool> MigrateSubscriptionAsync(V2SubscriptionDto request, CancellationToken cancellationToken = default);
    }
}
