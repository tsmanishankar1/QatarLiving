using Dapr.Actors;
using QLN.Common.DTO_s;
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
    #region Actor Interfaces

    public interface IV2SubscriptionService
    {
        #region Subscription Operations

        /// <summary>
        /// Gets active subscriptions by vertical type for a specific user
        /// </summary>
        /// <param name="verticalTypeId">Vertical type ID</param>
        /// <param name="userid">User ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Grouped subscription response</returns>
        Task<V2SubscriptionGroupResponseDto> GetSubscriptionsByVerticalAsync(
            int verticalTypeId,
            string userid,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Purchases a subscription based on product code (user buys a product)
        /// Creates the subscription through actor which handles DB persistence and events
        /// </summary>
        /// <param name="request">Purchase request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>New subscription ID</returns>
        Task<Guid> PurchaseSubscriptionAsync(
            V2SubscriptionPurchaseRequestDto request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all subscriptions for a specific user with optional filtering
        /// </summary>
        /// <param name="vertical">Optional vertical filter</param>
        /// <param name="subVertical">Optional sub-vertical filter</param>
        /// <param name="userId">User ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of user subscriptions</returns>
        Task<List<V2SubscriptionResponseDto>> GetUserSubscriptionsAsync(
            Vertical? vertical,
            SubVertical? subVertical,
            string userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all active subscriptions for a specific user
        /// </summary>
        /// <param name="userid">User ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of all active subscriptions for the user</returns>
        Task<List<V2SubscriptionResponseDto>> GetAllActiveSubscriptionsAsync(
            string userid,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancels a subscription through actor (marks as cancelled and publishes events)
        /// </summary>
        /// <param name="subscriptionId">Subscription ID</param>
        /// <param name="userid">User ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful</returns>
        Task<bool> CancelSubscriptionAsync(
            Guid subscriptionId,
            string userid,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates if a subscription has enough quota for the requested usage through actor
        /// </summary>
        /// <param name="subscriptionId">Subscription ID</param>
        /// <param name="quotaType">Type of quota (e.g., "ads_budget")</param>
        /// <param name="requestedAmount">Amount to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if quota is available</returns>
        Task<bool> ValidateSubscriptionUsageAsync(
            Guid subscriptionId,
            string quotaType,
            int requestedAmount,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Records usage against a subscription quota through actor (updates both actor and DB)
        /// </summary>
        /// <param name="subscriptionId">Subscription ID</param>
        /// <param name="quotaType">Type of quota (e.g., "ads_budget")</param>
        /// <param name="amount">Amount to record</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful</returns>
        Task<bool> RecordSubscriptionUsageAsync(
            Guid subscriptionId,
            string quotaType,
            int amount,
            CancellationToken cancellationToken = default);

        #endregion

        #region Addon Operations

        /// <summary>
        /// Purchases an addon based on product code through actor
        /// Creates the addon through actor which handles DB persistence and events
        /// </summary>
        /// <param name="request">Addon purchase request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>New addon ID</returns>
        Task<Guid> PurchaseAddonAsync(
            V2UserAddonPurchaseRequestDto request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates if an addon has enough quota for the requested usage through actor
        /// </summary>
        /// <param name="addonId">Addon ID</param>
        /// <param name="quotaType">Type of quota</param>
        /// <param name="requestedAmount">Amount to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if quota is available</returns>
        Task<bool> ValidateAddonUsageAsync(
            Guid addonId,
            string quotaType,
            int requestedAmount,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Records usage against an addon quota through actor
        /// </summary>
        /// <param name="addonId">Addon ID</param>
        /// <param name="quotaType">Type of quota</param>
        /// <param name="amount">Amount to record</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful</returns>
        Task<bool> RecordAddonUsageAsync(
            Guid addonId,
            string quotaType,
            int amount,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all addons for a specific user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of user addons</returns>
        Task<List<V2UserAddonResponseDto>> GetUserAddonsAsync(
            string userId,
            CancellationToken cancellationToken = default);

        #endregion

        #region Expiration Management (for scheduler)

        /// <summary>
        /// Gets list of subscription IDs that have expired (for scheduler)
        /// Reads from DB to find expired subscriptions
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of expired subscription IDs</returns>
        Task<List<Guid>> GetExpiredSubscriptionsAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks a subscription as expired through actor (called by scheduler)
        /// Actor handles DB update and event publishing
        /// </summary>
        /// <param name="subscriptionId">Subscription ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful</returns>
        Task<bool> MarkSubscriptionAsExpiredAsync(
            Guid subscriptionId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets list of addon IDs that have expired (for scheduler)
        /// Reads from DB to find expired addons
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of expired addon IDs</returns>
        Task<List<Guid>> GetExpiredAddonsAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks an addon as expired through actor (called by scheduler)
        /// Actor handles DB update and event publishing
        /// </summary>
        /// <param name="addonId">Addon ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful</returns>
        Task<bool> MarkAddonAsExpiredAsync(
            Guid addonId,
            CancellationToken cancellationToken = default);

        #endregion

        #region Advanced Operations (Optional)

        /// <summary>
        /// Gets subscriptions with filtering and pagination
        /// Uses DB for filtering, then gets actor data for each result
        /// </summary>
        /// <param name="filter">Filter criteria</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paginated subscription results</returns>
        Task<V2PaginatedResponseDto<V2SubscriptionResponseDto>> GetSubscriptionsAsync(
            V2SubscriptionFilterDto filter,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets subscription by ID through actor
        /// </summary>
        /// <param name="subscriptionId">Subscription ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Subscription details or null</returns>
        Task<V2SubscriptionResponseDto?> GetSubscriptionByIdAsync(
            Guid subscriptionId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets addon by ID through actor
        /// </summary>
        /// <param name="addonId">Addon ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Addon details or null</returns>
        Task<V2UserAddonResponseDto?> GetAddonByIdAsync(
            Guid addonId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Extends subscription end date through actor
        /// Actor handles DB update and state sync
        /// </summary>
        /// <param name="subscriptionId">Subscription ID</param>
        /// <param name="additionalDuration">Duration to add</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful</returns>
        Task<bool> ExtendSubscriptionAsync(
            Guid subscriptionId,
            TimeSpan additionalDuration,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Refills subscription quota through actor
        /// Actor handles DB update and state sync
        /// </summary>
        /// <param name="subscriptionId">Subscription ID</param>
        /// <param name="quotaType">Type of quota</param>
        /// <param name="amount">Amount to add</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful</returns>
        Task<bool> RefillSubscriptionQuotaAsync(
            Guid subscriptionId,
            string quotaType,
            decimal amount,
            CancellationToken cancellationToken = default);

        #endregion
        // Admin subscription operations
        Task<bool> UpdateSubscriptionStatusAsync(Guid subscriptionId, SubscriptionStatus newStatus, CancellationToken cancellationToken = default);
        Task<bool> UpdateSubscriptionEndDateAsync(Guid subscriptionId, DateTime newEndDate, CancellationToken cancellationToken = default);
        Task<bool> AdminCancelSubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default);

        // Admin addon operations
        Task<bool> UpdateAddonStatusAsync(Guid addonId, SubscriptionStatus newStatus, CancellationToken cancellationToken = default);
        Task<bool> UpdateAddonEndDateAsync(Guid addonId, DateTime newEndDate, CancellationToken cancellationToken = default);
        Task<bool> ExtendAddonAsync(Guid addonId, TimeSpan additionalDuration, CancellationToken cancellationToken = default);
        Task<bool> RefillAddonQuotaAsync(Guid addonId, string quotaType, decimal amount, CancellationToken cancellationToken = default);
        Task<bool> CancelAddonAsync(Guid addonId, string userId, CancellationToken cancellationToken = default);
        Task<bool> AdminCancelAddonAsync(Guid addonId, CancellationToken cancellationToken = default);
        Task<Guid> MigrateSubscriptionAsync(Guid subscriptionId, V2SubscriptionDto request, CancellationToken cancellationToken = default);
        Task<Guid> PurchaseFreeAdsSubscriptionAsync(V2SubscriptionPurchaseRequestDto request, CancellationToken cancellationToken = default);
        Task<bool> ValidateFreeAdsUsageAsync(Guid subscriptionId, string category, string? l1Category, string? l2Category, int requestedAmount, CancellationToken cancellationToken = default);
        Task<bool> RecordFreeAdsUsageAsync(Guid subscriptionId, string category, string? l1Category, string? l2Category, int amount, CancellationToken cancellationToken = default);
        Task<List<FreeAdsCategorySummary>> GetFreeAdsUsageSummaryAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
        Task<int> GetRemainingFreeAdsQuotaAsync(Guid subscriptionId, string category, string? l1Category, string? l2Category, CancellationToken cancellationToken = default);
        Task<int> GetRemainingFreeAdsQuotaForUserAsync(string userId, string category, string? l1Category, string? l2Category, CancellationToken cancellationToken = default);
        Task<List<V2SubscriptionResponseDto>> GetUserFreeSubscriptionsAsync(string userId, CancellationToken cancellationToken = default);

    }
    #endregion
}