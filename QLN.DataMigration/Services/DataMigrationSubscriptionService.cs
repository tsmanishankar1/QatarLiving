using Dapr.Actors;
using Dapr.Actors.Client;
using QLN.Common.DTO_s;
using QLN.Common.DTO_s.Subscription;
using QLN.Common.DTOs;
using QLN.Common.Infrastructure.IService.IProductService;
using QLN.Common.Infrastructure.QLDbContext;
using QLN.Common.Infrastructure.Subscriptions;
using System.Collections.Concurrent;

namespace QLN.DataMigration.Services
{
    public class DataMigrationSubscriptionService : IV2SubscriptionService
    {
        private readonly ILogger<DataMigrationSubscriptionService> _logger;
        private readonly IActorProxyFactory _actorProxyFactory;

        public DataMigrationSubscriptionService(
            ILogger<DataMigrationSubscriptionService> logger,
            IActorProxyFactory actorProxyFactory
            )
        {
            _logger = logger;
            _actorProxyFactory = actorProxyFactory;
        }

        #region Actor Proxy Helpers

        private IV2SubscriptionActor GetV2SubscriptionActorProxy(Guid id)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("V2 Actor ID cannot be empty", nameof(id));

            return _actorProxyFactory.CreateActorProxy<IV2SubscriptionActor>(
                new ActorId(id.ToString()),
                "V2SubscriptionActor");
        }
        #endregion

        public async Task<Guid> MigrateSubscriptionAsync(Guid subscriptionId, V2SubscriptionDto request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            _logger.LogInformation("Purchasing V2 subscription for user {UserId} with product {ProductCode}", request.UserId, request.ProductCode);

            var actor = GetV2SubscriptionActorProxy(subscriptionId);

            // Actor handles all DB operations, transactions, and event publishing
            var success = await actor.MigrateSubscriptionAsync(request, cancellationToken);
            if (!success)
            {
                throw new InvalidOperationException("Failed to create subscription via actor");
            }

            _logger.LogInformation("V2 Subscription created successfully: {SubscriptionId}", subscriptionId);
            return subscriptionId;
        }

        public Task<bool> AdminCancelAddonAsync(Guid addonId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> AdminCancelSubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> CancelAddonAsync(Guid addonId, string userId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> CancelSubscriptionAsync(Guid subscriptionId, string userid, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ExtendAddonAsync(Guid addonId, TimeSpan additionalDuration, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ExtendSubscriptionAsync(Guid subscriptionId, TimeSpan additionalDuration, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<V2UserAddonResponseDto?> GetAddonByIdAsync(Guid addonId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<List<V2SubscriptionResponseDto>> GetAllActiveSubscriptionsAsync(string userid, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<List<Guid>> GetExpiredAddonsAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<List<Guid>> GetExpiredSubscriptionsAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<V2SubscriptionResponseDto?> GetSubscriptionByIdAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<V2PaginatedResponseDto<V2SubscriptionResponseDto>> GetSubscriptionsAsync(V2SubscriptionFilterDto filter, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<V2SubscriptionGroupResponseDto> GetSubscriptionsByVerticalAsync(int verticalTypeId, string userid, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<List<V2UserAddonResponseDto>> GetUserAddonsAsync(string userId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<List<V2SubscriptionResponseDto>> GetUserSubscriptionsAsync(Vertical? vertical, SubVertical? subVertical, string userId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> MarkAddonAsExpiredAsync(Guid addonId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> MarkSubscriptionAsExpiredAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Guid> PurchaseAddonAsync(V2UserAddonPurchaseRequestDto request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Guid> PurchaseSubscriptionAsync(V2SubscriptionPurchaseRequestDto request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> RecordAddonUsageAsync(Guid addonId, string quotaType, int amount, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> RecordSubscriptionUsageAsync(Guid subscriptionId, string quotaType, int amount, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> RefillAddonQuotaAsync(Guid addonId, string quotaType, decimal amount, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> RefillSubscriptionQuotaAsync(Guid subscriptionId, string quotaType, decimal amount, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateAddonEndDateAsync(Guid addonId, DateTime newEndDate, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateAddonStatusAsync(Guid addonId, SubscriptionStatus newStatus, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateSubscriptionEndDateAsync(Guid subscriptionId, DateTime newEndDate, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateSubscriptionStatusAsync(Guid subscriptionId, SubscriptionStatus newStatus, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ValidateAddonUsageAsync(Guid addonId, string quotaType, int requestedAmount, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ValidateSubscriptionUsageAsync(Guid subscriptionId, string quotaType, int requestedAmount, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Guid> PurchaseFreeAdsSubscriptionAsync(V2SubscriptionPurchaseRequestDto request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ValidateFreeAdsUsageAsync(Guid subscriptionId, string category, string? l1Category, string? l2Category, int requestedAmount, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> RecordFreeAdsUsageAsync(Guid subscriptionId, string category, string? l1Category, string? l2Category, int amount, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<List<FreeAdsCategorySummary>> GetFreeAdsUsageSummaryAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<int> GetRemainingFreeAdsQuotaAsync(Guid subscriptionId, string category, string? l1Category, string? l2Category, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<List<V2SubscriptionResponseDto>> GetUserFreeSubscriptionsAsync(string userId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
