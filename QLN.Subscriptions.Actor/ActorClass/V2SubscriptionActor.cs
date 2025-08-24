using Dapr.Actors.Runtime;
using Dapr.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s.Payments;
using QLN.Common.DTO_s.Subscription;
using QLN.Common.Infrastructure.IService.IProductService;
using QLN.Common.Infrastructure.Model;
using QLN.Common.Infrastructure.QLDbContext;
using QLN.Common.Infrastructure.Subscriptions;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using static QLN.Common.DTO_s.Subscription.SubscriptionQuota;

namespace QLN.Subscriptions.Actor.ActorClass
{
    /// <summary>
    /// V2 Subscription Actor - Always syncs from database on all read operations
    /// Ensures manual database changes are immediately reflected in all API responses
    /// </summary>
    public class V2SubscriptionActor : Dapr.Actors.Runtime.Actor, IV2SubscriptionActor, IRemindable
    {
        private const string StateKey = "v2-subscription-data";
        private const string ReminderName = "CheckExpiryReminder";
        private const string PubSubName = "pubsub";

        private readonly ILogger<V2SubscriptionActor> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        // Memory cache for actor state - only used for brief performance optimization
        private static readonly ConcurrentDictionary<string, V2SubscriptionDto> _memoryCache = new();

        public V2SubscriptionActor(
            ActorHost host,
            ILogger<V2SubscriptionActor> logger,
            IServiceScopeFactory scopeFactory
        ) : base(host)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        }

        #region Actor Lifecycle

        protected override async Task OnActivateAsync()
        {
            await base.OnActivateAsync();

            // Register reminder for expiry checking
            await RegisterReminderAsync(
                ReminderName,
                null,
                GetDelayUntilMidnightUtc(),
                TimeSpan.FromDays(1));

            _logger.LogInformation("V2SubscriptionActor {ActorId} activated with expiry reminder", this.Id.GetId());
        }

        protected override async Task OnDeactivateAsync()
        {
            // Clean up memory cache entry
            var key = this.Id.GetId();
            _memoryCache.TryRemove(key, out _);

            await base.OnDeactivateAsync();
            _logger.LogInformation("V2SubscriptionActor {ActorId} deactivated", this.Id.GetId());
        }

        public async Task ReceiveReminderAsync(string reminderName, byte[] state, TimeSpan dueTime, TimeSpan period)
        {
            if (!string.Equals(reminderName, ReminderName, StringComparison.Ordinal))
                return;

            try
            {
                // Always force sync for reminder checks
                var data = await SyncFromDatabaseAsync(force: true, CancellationToken.None);
                if (data == null) return;

                if (data.StatusId != SubscriptionStatus.Expired && data.EndDate <= DateTime.UtcNow)
                {
                    _logger.LogInformation("Subscription {Id} expired by reminder, marking as expired", data.Id);
                    await MarkAsExpiredAsync(CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in expiry reminder for subscription {Id}", this.Id.GetId());
            }
        }

        private static TimeSpan GetDelayUntilMidnightUtc()
        {
            var now = DateTime.UtcNow;
            var nextMidnight = now.Date.AddDays(1);
            return nextMidnight - now;
        }

        #endregion

        #region Public Actor API

        public async Task<bool> CreateSubscriptionAsync(V2SubscriptionPurchaseRequestDto request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            _logger.LogInformation("Creating subscription for user {UserId} with product {ProductCode}",
                request.UserId, request.ProductCode);

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<QLSubscriptionContext>();
            var daprClient = scope.ServiceProvider.GetRequiredService<DaprClient>();

            using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var subscriptionId = Guid.Parse(this.Id.GetId());

                // Validate product
                var product = await context.Products
                    .FirstOrDefaultAsync(p => p.ProductCode == request.ProductCode && p.IsActive, cancellationToken);

                if (product == null)
                    throw new InvalidOperationException($"Product with code {request.ProductCode} not found or inactive");

                if (product.ProductType != ProductType.SUBSCRIPTION && product.ProductType != ProductType.PUBLISH)
                    throw new InvalidOperationException($"Product {request.ProductCode} is not a subscription or P2P product");

                // Create database subscription
                var dbSubscription = new Subscription
                {
                    SubscriptionId = subscriptionId,
                    ProductCode = product.ProductCode,
                    ProductName = product.ProductName,
                    ProductType = product.ProductType,
                    UserId = request.UserId,
                    AdId = request.AdId,
                    CompanyId = request.CompanyId,
                    PaymentId = request.PaymentId,
                    Vertical = product.Vertical,
                    SubVertical = product.SubVertical,
                    Quota = BuildSubscriptionQuotaFromProduct(product),
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.Add(GetDurationFromProduct(product)),
                    Status = SubscriptionStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.Subscriptions.Add(dbSubscription);
                await context.SaveChangesAsync(cancellationToken);

                // Create actor state
                var v2Dto = new V2SubscriptionDto
                {
                    Id = subscriptionId,
                    ProductCode = product.ProductCode,
                    ProductName = product.ProductName,
                    ProductType = product.ProductType,
                    UserId = request.UserId,
                    CompanyId = request.CompanyId,
                    PaymentId = request.PaymentId,
                    Vertical = product.Vertical,
                    SubVertical = product.SubVertical,
                    Price = product.Price,
                    Currency = product.Currency,
                    Quota = dbSubscription.Quota,
                    StartDate = dbSubscription.StartDate,
                    EndDate = dbSubscription.EndDate,
                    StatusId = SubscriptionStatus.Active,
                    lastUpdated = DateTime.UtcNow,
                    Version = "V2"
                };

                await FastSetDataAsync(v2Dto, cancellationToken);

                // Commit transaction before publishing events
                await transaction.CommitAsync(cancellationToken);

                // Clear cache to ensure fresh reads
                ClearCache();

                _logger.LogInformation("V2 Subscription created successfully: {Id} for user: {UserId}",
                    subscriptionId, request.UserId);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to create V2 subscription for user {UserId}", request.UserId);
                throw;
            }
        }

        public async Task<bool> MigrateSubscriptionAsync(V2SubscriptionDto request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            _logger.LogInformation("Creating subscription for user {UserId} with product {ProductCode}",
                request.UserId, request.ProductCode);

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<QLSubscriptionContext>();
            var daprClient = scope.ServiceProvider.GetRequiredService<DaprClient>();

            using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var subscriptionId = Guid.Parse(this.Id.GetId());

                // Validate product
                var product = await context.Products
                    .FirstOrDefaultAsync(p => p.ProductCode == request.ProductCode && p.IsActive, cancellationToken);

                if (product == null)
                    throw new InvalidOperationException($"Product with code {request.ProductCode} not found or inactive");

                if (product.ProductType != ProductType.SUBSCRIPTION && product.ProductType != ProductType.PUBLISH)
                    throw new InvalidOperationException($"Product {request.ProductCode} is not a subscription or P2P product");

                // Create database subscription
                var dbSubscription = new Subscription
                {
                    SubscriptionId = subscriptionId,
                    ProductCode = product.ProductCode,
                    ProductName = product.ProductName,
                    UserId = request.UserId,
                    CompanyId = request.CompanyId,
                    PaymentId = request.PaymentId,
                    Vertical = product.Vertical,
                    SubVertical = product.SubVertical,
                    //Quota = BuildSubscriptionQuotaFromProduct(product),
                    Quota = request.Quota,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    Status = request.StatusId,
                    CreatedAt = request.StartDate, // default to the Start Date
                    UpdatedAt = DateTime.UtcNow
                };

                context.Subscriptions.Add(dbSubscription);
                await context.SaveChangesAsync(cancellationToken);

                await FastSetDataAsync(request, cancellationToken);

                // Commit transaction before publishing events
                await transaction.CommitAsync(cancellationToken);

                // Clear cache to ensure fresh reads
                ClearCache();

                _logger.LogInformation("V2 Subscription created successfully: {Id} for user: {UserId}",
                    subscriptionId, request.UserId);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to create V2 subscription for user {UserId}", request.UserId);
                throw;
            }
        }

        public async Task<bool> FastSetDataAsync(V2SubscriptionDto data, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(data);

            data.lastUpdated = DateTime.UtcNow;
            var key = this.Id.GetId();

            // Update memory cache
            _memoryCache[key] = data;

            // Save to actor state
            await StateManager.SetStateAsync(StateKey, data, cancellationToken);
            await StateManager.SaveStateAsync(cancellationToken);

            return true;
        }

        public Task<bool> SetDataAsync(V2SubscriptionDto data, CancellationToken cancellationToken = default)
            => FastSetDataAsync(data, cancellationToken);

        /// <summary>
        /// ALWAYS syncs from database to ensure manual changes are reflected
        /// This method is called by all GET endpoints to ensure fresh data
        /// </summary>
        public async Task<V2SubscriptionDto?> GetDataAsync(CancellationToken cancellationToken = default)
        {
            // ALWAYS force sync from database for all read operations
            return await SyncFromDatabaseAsync(force: true, cancellationToken);
        }

        /// <summary>
        /// ALWAYS syncs from database before validation to ensure latest status
        /// </summary>
        public async Task<bool> ValidateUsageAsync(string quotaType, decimal requestedAmount, CancellationToken cancellationToken = default)
        {
            // ALWAYS sync from database first to catch manual status changes
            var data = await SyncFromDatabaseAsync(force: true, cancellationToken);
            if (data == null)
            {
                _logger.LogWarning("Cannot validate usage: subscription data not found for {Id}", this.Id.GetId());
                return false;
            }

            if (data.StatusId != SubscriptionStatus.Active || data.EndDate <= DateTime.UtcNow)
            {
                _logger.LogWarning("Cannot validate usage: subscription {Id} is not active (Status: {Status}) or expired (EndDate: {EndDate})",
                    data.Id, data.StatusId, data.EndDate);
                return false;
            }

            var qty = (int)Math.Ceiling(requestedAmount);
            var action = MapQuotaTypeToAction(quotaType);
            var validationResult = data.Quota.ValidateAction(action, qty);

            _logger.LogInformation("Usage validation for subscription {Id}: quotaType={QuotaType}, amount={Amount}, valid={IsValid}",
                data.Id, quotaType, qty, validationResult.IsValid);

            return validationResult.IsValid;
        }

        public async Task<bool> RecordUsageAsync(string quotaType, decimal amount, CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<QLSubscriptionContext>();

            using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // ALWAYS sync from database first to catch manual changes
                var data = await SyncFromDatabaseAsync(force: true, cancellationToken);
                if (data == null)
                {
                    _logger.LogWarning("Cannot record usage: subscription data not found for {Id}", this.Id.GetId());
                    return false;
                }

                if (data.StatusId != SubscriptionStatus.Active || data.EndDate <= DateTime.UtcNow)
                {
                    _logger.LogWarning("Cannot record usage: subscription {Id} is not active (Status: {Status}) or expired (EndDate: {EndDate})",
                        data.Id, data.StatusId, data.EndDate);
                    return false;
                }

                var qty = (int)Math.Ceiling(amount);
                var action = MapQuotaTypeToAction(quotaType);

                // Record usage in actor state
                var success = data.Quota.RecordUsage(action, qty);
                if (!success)
                {
                    _logger.LogWarning("Failed to record usage in actor state for subscription {Id}", data.Id);
                    return false;
                }

                // Update database
                var subscriptionId = Guid.Parse(this.Id.GetId());
                var dbSubscription = await context.Subscriptions
                    .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId, cancellationToken);

                if (dbSubscription != null)
                {
                    dbSubscription.Quota.RecordUsage(action, qty);
                    dbSubscription.UpdatedAt = DateTime.UtcNow;
                    await context.SaveChangesAsync(cancellationToken);
                }

                // Update actor state
                await FastSetDataAsync(data, cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                // Clear cache after update
                ClearCache();

                _logger.LogInformation("Usage recorded successfully for subscription {Id}: quotaType={QuotaType}, amount={Amount}",
                    data.Id, quotaType, qty);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error recording usage for subscription {Id}", this.Id.GetId());
                return false;
            }
        }

        /// <summary>
        /// ALWAYS syncs from database before checking status
        /// </summary>
        public async Task<bool> IsActiveAsync(CancellationToken cancellationToken = default)
        {
            // ALWAYS sync from database to get latest status
            var data = await SyncFromDatabaseAsync(force: true, cancellationToken);
            if (data == null) return false;

            var isActive = data.StatusId == SubscriptionStatus.Active && data.EndDate > DateTime.UtcNow;
            _logger.LogDebug("Subscription {Id} active status: {IsActive} (Status: {Status}, EndDate: {EndDate})",
                data.Id, isActive, data.StatusId, data.EndDate);
            return isActive;
        }

        public async Task<bool> CancelSubscriptionAsync(string userId, CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<QLSubscriptionContext>();
            var daprClient = scope.ServiceProvider.GetRequiredService<DaprClient>();

            using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var subscriptionId = Guid.Parse(this.Id.GetId());

                var dbSubscription = await context.Subscriptions
                    .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId && s.UserId == userId, cancellationToken);

                if (dbSubscription == null)
                {
                    _logger.LogWarning("Cannot cancel: subscription {Id} not found for user {UserId}", subscriptionId, userId);
                    return false;
                }

                // Update database
                dbSubscription.Status = SubscriptionStatus.Cancelled;
                dbSubscription.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync(cancellationToken);

                // Update actor state with fresh data from database
                var existingData = await SyncFromDatabaseAsync(force: true, cancellationToken);
                if (existingData != null)
                {
                    existingData.StatusId = SubscriptionStatus.Cancelled;
                    existingData.lastUpdated = DateTime.UtcNow;
                    await FastSetDataAsync(existingData, cancellationToken);

                    await transaction.CommitAsync(cancellationToken);

                    // Clear cache after update
                    ClearCache();

                    // Publish subscription cancelled event
                    await PublishSubscriptionCancelledEventAsync(existingData, daprClient, cancellationToken);
                }
                else
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                _logger.LogInformation("V2 Subscription {Id} cancelled for user {UserId}", subscriptionId, userId);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error cancelling V2 subscription {Id} for user {UserId}", this.Id.GetId(), userId);
                throw;
            }
        }

        public async Task<bool> ExtendSubscriptionAsync(TimeSpan additionalDuration, CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<QLSubscriptionContext>();

            using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var subscriptionId = Guid.Parse(this.Id.GetId());

                var dbSubscription = await context.Subscriptions
                    .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId, cancellationToken);

                if (dbSubscription == null)
                {
                    _logger.LogWarning("Cannot extend: subscription {Id} not found", subscriptionId);
                    return false;
                }

                // Update database
                dbSubscription.EndDate = dbSubscription.EndDate.Add(additionalDuration);
                dbSubscription.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync(cancellationToken);

                // Update actor state with fresh data
                var actorData = await SyncFromDatabaseAsync(force: true, cancellationToken);
                if (actorData != null)
                {
                    actorData.EndDate = dbSubscription.EndDate;
                    actorData.lastUpdated = DateTime.UtcNow;
                    await FastSetDataAsync(actorData, cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);

                // Clear cache after update
                ClearCache();

                _logger.LogInformation("Subscription {Id} extended by {Duration}", subscriptionId, additionalDuration);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error extending subscription {Id}", this.Id.GetId());
                return false;
            }
        }

        public async Task<bool> RefillQuotaAsync(string quotaType, decimal amount, CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<QLSubscriptionContext>();

            using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var subscriptionId = Guid.Parse(this.Id.GetId());

                var dbSubscription = await context.Subscriptions
                    .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId, cancellationToken);

                if (dbSubscription == null)
                {
                    _logger.LogWarning("Cannot refill quota: subscription {Id} not found", subscriptionId);
                    return false;
                }

                // Update database quota
                var qty = (int)Math.Ceiling(amount);
                switch ((quotaType ?? string.Empty).ToLower())
                {
                    case V2QuotaTypes.AdsBudget:
                        dbSubscription.Quota.TotalAdsAllowed += qty;
                        break;
                    case V2QuotaTypes.PromoteBudget:
                        dbSubscription.Quota.TotalPromotionsAllowed += qty;
                        break;
                    case V2QuotaTypes.FeatureBudget:
                        dbSubscription.Quota.TotalFeaturesAllowed += qty;
                        break;
                    case V2QuotaTypes.RefreshBudget:
                        dbSubscription.Quota.DailyRefreshesAllowed += qty;
                        break;
                    case V2QuotaTypes.SocialMediaPosts:
                        dbSubscription.Quota.SocialMediaPostsAllowed += qty;
                        break;
                    default:
                        _logger.LogWarning("Unknown quotaType '{QuotaType}' in refill for subscription {Id}", quotaType, subscriptionId);
                        return false;
                }

                dbSubscription.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync(cancellationToken);

                // Update actor state with fresh data
                var actorData = await SyncFromDatabaseAsync(force: true, cancellationToken);
                if (actorData != null)
                {
                    actorData.Quota = dbSubscription.Quota;
                    actorData.lastUpdated = DateTime.UtcNow;
                    await FastSetDataAsync(actorData, cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);

                // Clear cache after update
                ClearCache();

                _logger.LogInformation("Subscription {Id} quota {QuotaType} refilled by {Amount}", subscriptionId, quotaType, amount);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error refilling subscription {Id} quota", this.Id.GetId());
                return false;
            }
        }

        public async Task<bool> MarkAsExpiredAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<QLSubscriptionContext>();
            var daprClient = scope.ServiceProvider.GetRequiredService<DaprClient>();

            using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var subscriptionId = Guid.Parse(this.Id.GetId());

                var dbSubscription = await context.Subscriptions
                    .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId, cancellationToken);

                if (dbSubscription == null)
                {
                    _logger.LogWarning("Cannot mark as expired: subscription {Id} not found", subscriptionId);
                    return false;
                }

                // Update database
                dbSubscription.Status = SubscriptionStatus.Expired;
                dbSubscription.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync(cancellationToken);

                // Update actor state with fresh data
                var existingData = await SyncFromDatabaseAsync(force: true, cancellationToken);
                if (existingData != null)
                {
                    existingData.StatusId = SubscriptionStatus.Expired;
                    existingData.lastUpdated = DateTime.UtcNow;
                    await FastSetDataAsync(existingData, cancellationToken);

                    await transaction.CommitAsync(cancellationToken);

                    // Clear cache after update
                    ClearCache();

                    // Publish subscription expired event
                    await PublishSubscriptionExpiredEventAsync(existingData, daprClient, cancellationToken);
                }
                else
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                _logger.LogInformation("Subscription {Id} marked as expired", subscriptionId);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error marking subscription {Id} as expired", this.Id.GetId());
                return false;
            }
        }

        #endregion

        #region Cache Management

        private void ClearCache()
        {
            var key = this.Id.GetId();
            _memoryCache.TryRemove(key, out _);
            _logger.LogDebug("Cleared cache for subscription {Id}", key);
        }

        #endregion

        #region Event Publishing

        private async Task PublishSubscriptionCancelledEventAsync(V2SubscriptionDto data, DaprClient daprClient, CancellationToken cancellationToken)
        {
            try
            {
                var topic = GetCancelledTopicForVertical(data.Vertical);
                var eventData = new V2SubscriptionCancelledEventDto
                {
                    SubscriptionId = data.Id,
                    ProductCode = data.ProductCode,
                    UserId = data.UserId ?? string.Empty,
                    Vertical = data.Vertical,
                    SubVertical = data.SubVertical,
                    CancelledAt = DateTime.UtcNow,
                    EventId = Guid.NewGuid(),
                    Version = "V2",
                    Metadata = new System.Collections.Generic.Dictionary<string, object>
                    {
                        ["originalEndDate"] = data.EndDate,
                        ["daysRemaining"] = (data.EndDate - DateTime.UtcNow).TotalDays
                    }
                };

                await daprClient.PublishEventAsync(PubSubName, topic, eventData, cancellationToken);
                _logger.LogInformation("Published {Topic} for subscription {Id} (Vertical={Vertical}, SubVertical={SubVertical})",
                    topic, data.Id, data.Vertical, data.SubVertical?.ToString() ?? "null");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish subscription cancelled event for {Id}", data.Id);
            }
        }

        private async Task PublishSubscriptionExpiredEventAsync(V2SubscriptionDto data, DaprClient daprClient, CancellationToken cancellationToken)
        {
            try
            {
                var topic = GetExpiredTopicForVertical(data.Vertical);
                var eventData = new V2SubscriptionExpiredEventDto
                {
                    SubscriptionId = data.Id,
                    ProductCode = data.ProductCode,
                    UserId = data.UserId ?? string.Empty,
                    Vertical = data.Vertical,
                    SubVertical = data.SubVertical,
                    ExpiredAt = DateTime.UtcNow,
                    EventId = Guid.NewGuid(),
                    Version = "V2",
                    Metadata = new System.Collections.Generic.Dictionary<string, object>
                    {
                        ["actualEndDate"] = data.EndDate,
                        ["quotaUsage"] = new
                        {
                            adsUsed = data.Quota.AdsUsed,
                            promotionsUsed = data.Quota.PromotionsUsed,
                            featuresUsed = data.Quota.FeaturesUsed
                        }
                    }
                };

                await daprClient.PublishEventAsync(PubSubName, topic, eventData, cancellationToken);
                _logger.LogInformation("Published {Topic} for subscription {Id} (Vertical={Vertical}, SubVertical={SubVertical})",
                    topic, data.Id, data.Vertical, data.SubVertical?.ToString() ?? "null");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish subscription expired event for {Id}", data.Id);
            }
        }

        private static string GetCancelledTopicForVertical(Vertical vertical) => vertical switch
        {
            Vertical.Classifieds => "subscription.cancelled.classifieds",
            Vertical.Properties => "subscription.cancelled.properties",
            Vertical.Services => "subscription.cancelled.services",
            Vertical.Rewards => "subscription.cancelled.rewards",
            _ => "subscription.cancelled.unknown"
        };

        private static string GetExpiredTopicForVertical(Vertical vertical) => vertical switch
        {
            Vertical.Classifieds => "subscription.expired.classifieds",
            Vertical.Properties => "subscription.expired.properties",
            Vertical.Services => "subscription.expired.services",
            Vertical.Rewards => "subscription.expired.rewards",
            _ => "subscription.expired.unknown"
        };

        #endregion

        #region Database Sync and Helper Methods

        /// <summary>
        /// Syncs actor state from DB - database is always the source of truth
        /// When force=true, always reads from database to catch manual changes
        /// </summary>
        private async Task<V2SubscriptionDto?> SyncFromDatabaseAsync(bool force, CancellationToken cancellationToken)
        {
            var key = this.Id.GetId();

            // For performance, check cache first only if not forcing
            if (!force && _memoryCache.TryGetValue(key, out var cached))
            {
                _logger.LogDebug("Using cached data for subscription {Id}", key);
                return cached;
            }

            _logger.LogDebug("Syncing subscription {Id} from database (force: {Force})", key, force);

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<QLSubscriptionContext>();

            if (!Guid.TryParse(key, out var subscriptionId))
            {
                _logger.LogWarning("Invalid subscription ID format: {Id}", key);
                return null;
            }

            var dbSubscription = await context.Subscriptions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId, cancellationToken);

            if (dbSubscription == null)
            {
                _logger.LogWarning("Subscription {Id} not found in database during sync", key);
                return await TryReadFromActorStateAsync(cancellationToken);
            }

            var mappedFromDb = MapDbToV2Dto(dbSubscription);

            // Update actor state and cache with latest database data
            await FastSetDataAsync(mappedFromDb, cancellationToken);
            _logger.LogDebug("Synced subscription {Id} from database - Status: {Status}, EndDate: {EndDate}",
                subscriptionId, mappedFromDb.StatusId, mappedFromDb.EndDate);

            return mappedFromDb;
        }

        private async Task<V2SubscriptionDto?> TryReadFromActorStateAsync(CancellationToken cancellationToken)
        {
            var key = this.Id.GetId();

            // Check memory cache first
            if (_memoryCache.TryGetValue(key, out var cached))
                return cached;

            // Read from actor state store
            var stateResult = await StateManager.TryGetStateAsync<V2SubscriptionDto>(StateKey, cancellationToken);
            if (stateResult.HasValue && stateResult.Value != null)
            {
                _memoryCache[key] = stateResult.Value;
                return stateResult.Value;
            }

            return null;
        }

        private SubscriptionQuota BuildSubscriptionQuotaFromProduct(Product product)
        {
            var constraints = product.Constraints ?? new ProductConstraints();

            return new SubscriptionQuota
            {
                Vertical = product.Vertical.ToString(),
                Scope = constraints.Scope ?? "All",

                TotalAdsAllowed = constraints.AdsBudget ?? 0,
                TotalPromotionsAllowed = constraints.PromotedBudget ?? 0,
                TotalFeaturesAllowed = constraints.FeaturedBudget ?? 0,
                DailyRefreshesAllowed = constraints.RefreshBudgetPerDay ?? 0,
                RefreshesPerAdAllowed = constraints.RefreshBudgetPerAd ?? 1,
                SocialMediaPostsAllowed = 0,

                CanPublishAds = !(constraints.PayToPublish ?? false) || (constraints.AdsBudget ?? 0) > 0,
                CanPromoteAds = (constraints.PayToPromote ?? false) || (constraints.PromotedBudget ?? 0) > 0,
                CanFeatureAds = (constraints.PayToFeature ?? false) || (constraints.FeaturedBudget ?? 0) > 0,
                CanRefreshAds = (constraints.RefreshBudgetPerDay ?? 0) > 0,
                CanPostSocialMedia = false,

                RefreshInterval = product.ProductType == ProductType.FREE ? "Every 72 Hours" : "Every Hour",
                RefreshIntervalHours = product.ProductType == ProductType.FREE ? 72 : 1
            };
        }

        private TimeSpan GetDurationFromProduct(Product product)
        {
            if (product.Constraints?.Duration.HasValue == true)
            {
                return product.Constraints.Duration.Value;
            }

            return product.ProductType switch
            {
                ProductType.SUBSCRIPTION => TimeSpan.FromDays(30),
                ProductType.ADDON_COMBO => TimeSpan.FromDays(30),
                ProductType.ADDON_FEATURE => TimeSpan.FromDays(7),
                ProductType.ADDON_REFRESH => TimeSpan.FromDays(30),
                _ => TimeSpan.FromDays(30)
            };
        }

        private static string MapQuotaTypeToAction(string quotaType) =>
            (quotaType ?? string.Empty).ToLower() switch
            {
                V2QuotaTypes.AdsBudget => ActionTypes.Publish,
                V2QuotaTypes.PromoteBudget => ActionTypes.Promote,
                V2QuotaTypes.FeatureBudget => ActionTypes.Feature,
                V2QuotaTypes.RefreshBudget => ActionTypes.Refresh,
                V2QuotaTypes.SocialMediaPosts => ActionTypes.SocialMediaPost,
                _ => quotaType?.ToLower() ?? string.Empty
            };

        private static V2SubscriptionDto MapDbToV2Dto(Subscription dbSubscription)
        {
            return new V2SubscriptionDto
            {
                Id = dbSubscription.SubscriptionId,
                ProductCode = dbSubscription.ProductCode,
                ProductType = dbSubscription.ProductType,
                ProductName = dbSubscription.ProductName,
                UserId = dbSubscription.UserId,
                CompanyId = dbSubscription.CompanyId,
                PaymentId = dbSubscription.PaymentId,
                Vertical = dbSubscription.Vertical,
                SubVertical = dbSubscription.SubVertical,
                Price = 0, // Default price, could be enhanced to store actual price
                Currency = "QAR",
                Quota = dbSubscription.Quota,
                StartDate = dbSubscription.StartDate,
                EndDate = dbSubscription.EndDate,
                StatusId = dbSubscription.Status,
                lastUpdated = dbSubscription.UpdatedAt ?? dbSubscription.CreatedAt,
                Version = "V2"
            };
        }

        #endregion
        public async Task<bool> UpdateStatusAsync(SubscriptionStatus newStatus, CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<QLSubscriptionContext>();

            using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var subscriptionId = Guid.Parse(this.Id.GetId());

                var dbSubscription = await context.Subscriptions
                    .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId, cancellationToken);

                if (dbSubscription == null)
                {
                    _logger.LogWarning("Cannot update status: subscription {Id} not found", subscriptionId);
                    return false;
                }

                // Update database
                dbSubscription.Status = newStatus;
                dbSubscription.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync(cancellationToken);

                // Update actor state with fresh data
                var actorData = await SyncFromDatabaseAsync(force: true, cancellationToken);
                if (actorData != null)
                {
                    actorData.StatusId = newStatus;
                    actorData.lastUpdated = DateTime.UtcNow;
                    await FastSetDataAsync(actorData, cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);

                // Clear cache after update
                ClearCache();

                _logger.LogInformation("Subscription {Id} status updated to {Status}", subscriptionId, newStatus);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error updating subscription {Id} status", this.Id.GetId());
                return false;
            }
        }

        public async Task<bool> UpdateEndDateAsync(DateTime newEndDate, CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<QLSubscriptionContext>();

            using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var subscriptionId = Guid.Parse(this.Id.GetId());

                var dbSubscription = await context.Subscriptions
                    .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId, cancellationToken);

                if (dbSubscription == null)
                {
                    _logger.LogWarning("Cannot update end date: subscription {Id} not found", subscriptionId);
                    return false;
                }

                // Update database
                dbSubscription.EndDate = newEndDate;
                dbSubscription.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync(cancellationToken);

                // Update actor state with fresh data
                var actorData = await SyncFromDatabaseAsync(force: true, cancellationToken);
                if (actorData != null)
                {
                    actorData.EndDate = newEndDate;
                    actorData.lastUpdated = DateTime.UtcNow;
                    await FastSetDataAsync(actorData, cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);

                // Clear cache after update
                ClearCache();

                _logger.LogInformation("Subscription {Id} end date updated to {EndDate}", subscriptionId, newEndDate);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error updating subscription {Id} end date", this.Id.GetId());
                return false;
            }
        }

        public async Task<bool> AdminCancelSubscriptionAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<QLSubscriptionContext>();
            var daprClient = scope.ServiceProvider.GetRequiredService<DaprClient>();

            using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var subscriptionId = Guid.Parse(this.Id.GetId());

                var dbSubscription = await context.Subscriptions
                    .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId, cancellationToken);

                if (dbSubscription == null)
                {
                    _logger.LogWarning("Cannot cancel: subscription {Id} not found", subscriptionId);
                    return false;
                }

                // Update database (no user validation for admin)
                dbSubscription.Status = SubscriptionStatus.Cancelled;
                dbSubscription.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync(cancellationToken);

                // Update actor state with fresh data from database
                var existingData = await SyncFromDatabaseAsync(force: true, cancellationToken);
                if (existingData != null)
                {
                    existingData.StatusId = SubscriptionStatus.Cancelled;
                    existingData.lastUpdated = DateTime.UtcNow;
                    await FastSetDataAsync(existingData, cancellationToken);

                    await transaction.CommitAsync(cancellationToken);

                    // Clear cache after update
                    ClearCache();

                    // Publish subscription cancelled event
                    await PublishSubscriptionCancelledEventAsync(existingData, daprClient, cancellationToken);
                }
                else
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                _logger.LogInformation("V2 Subscription {Id} cancelled by admin", subscriptionId);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error admin cancelling V2 subscription {Id}", this.Id.GetId());
                throw;
            }
        }
        #region FREE Ads Specific Methods

        /// <summary>
        /// Validate FREE ads usage for specific category
        /// </summary>
        public async Task<bool> ValidateFreeAdsUsageAsync(string category, string? l1Category, string? l2Category, int requestedAmount, CancellationToken cancellationToken = default)
        {
            // ALWAYS sync from database first to catch manual changes
            var data = await SyncFromDatabaseAsync(force: true, cancellationToken);
            if (data == null)
            {
                _logger.LogWarning("Cannot validate free ads usage: subscription data not found for {Id}", this.Id.GetId());
                return false;
            }

            // Check if subscription is active
            if (data.StatusId != SubscriptionStatus.Active || data.EndDate <= DateTime.UtcNow)
            {
                _logger.LogWarning("Cannot validate free ads usage: subscription {Id} is not active (Status: {Status}) or expired (EndDate: {EndDate})",
                    data.Id, data.StatusId, data.EndDate);
                return false;
            }
            _logger.LogWarning("product type = {Type}", data.ProductType);
            // Check if this is a FREE product type
            if (data.ProductType != ProductType.FREE)
            {
                _logger.LogWarning("Cannot validate free ads usage: subscription {Id} is not a FREE product type", data.Id);
                return false;
            }

            // Find the specific category quota
            var categoryUsage = data.Quota.CategoryQuotas.FirstOrDefault(c =>
                c.Category == category &&
                c.L1Category == l1Category &&
                c.L2Category == l2Category);

            if (categoryUsage == null)
            {
                _logger.LogWarning("Category quota not found for {Category}/{L1}/{L2} in subscription {Id}",
                    category, l1Category, l2Category, data.Id);
                return false;
            }

            // Validate against category quota (for FREE products, we use AdsUsed/AdsAllowed)
            var isValid = categoryUsage.AdsUsed + requestedAmount <= categoryUsage.AdsAllowed;

            _logger.LogInformation("Free ads usage validation for subscription {Id}: category={Category}/{L1}/{L2}, amount={Amount}, valid={IsValid}, used={Used}, allowed={Allowed}",
                data.Id, category, l1Category, l2Category, requestedAmount, isValid, categoryUsage.AdsUsed, categoryUsage.AdsAllowed);

            return isValid;
        }

        /// <summary>
        /// Record FREE ads usage for specific category
        /// </summary>
        public async Task<bool> RecordFreeAdsUsageAsync(string category, string? l1Category, string? l2Category, int amount, CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<QLSubscriptionContext>();

            using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // ALWAYS sync from database first to catch manual changes
                var data = await SyncFromDatabaseAsync(force: true, cancellationToken);
                if (data == null)
                {
                    _logger.LogWarning("Cannot record free ads usage: subscription data not found for {Id}", this.Id.GetId());
                    return false;
                }

                if (data.StatusId != SubscriptionStatus.Active || data.EndDate <= DateTime.UtcNow)
                {
                    _logger.LogWarning("Cannot record free ads usage: subscription {Id} is not active (Status: {Status}) or expired (EndDate: {EndDate})",
                        data.Id, data.StatusId, data.EndDate);
                    return false;
                }

                if (data.ProductType != ProductType.FREE)
                {
                    _logger.LogWarning("Cannot record free ads usage: subscription {Id} is not a FREE product type", data.Id);
                    return false;
                }

                // Find the specific category quota
                var categoryUsage = data.Quota.CategoryQuotas.FirstOrDefault(c =>
                    c.Category == category &&
                    c.L1Category == l1Category &&
                    c.L2Category == l2Category);

                if (categoryUsage == null)
                {
                    _logger.LogWarning("Category quota not found for {Category}/{L1}/{L2} in subscription {Id}",
                        category, l1Category, l2Category, data.Id);
                    return false;
                }

                // Validate before recording
                if (categoryUsage.AdsUsed + amount > categoryUsage.AdsAllowed)
                {
                    _logger.LogWarning("Free ads quota exceeded for category {Category}/{L1}/{L2} in subscription {Id}. Used: {Used}, Requested: {Amount}, Allowed: {Allowed}",
                        category, l1Category, l2Category, data.Id, categoryUsage.AdsUsed, amount, categoryUsage.AdsAllowed);
                    return false;
                }

                // Update database using interpolated SQL - this avoids the format string issue
                var subscriptionId = Guid.Parse(this.Id.GetId());
                var updatedAt = DateTime.UtcNow;
                var l1CategoryValue = l1Category ?? "";
                var l2CategoryValue = l2Category ?? "";

                var rowsAffected = await context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE ""Subscriptions"" 
            SET ""Quota"" = jsonb_set(
                jsonb_set(""Quota"", '{{""AdsUsed""}}', (COALESCE((""Quota""->>'AdsUsed')::int, 0) + {amount})::text::jsonb),
                '{{""CategoryQuotas""}}', 
                (
                    SELECT jsonb_agg(
                        CASE 
                            WHEN category_item->>'Category' = {category} 
                                AND COALESCE(category_item->>'L1Category', '') = COALESCE({l1CategoryValue}, '')
                                AND COALESCE(category_item->>'L2Category', '') = COALESCE({l2CategoryValue}, '')
                            THEN jsonb_set(category_item, '{{""AdsUsed""}}', ((category_item->>'AdsUsed')::int + {amount})::text::jsonb)
                            ELSE category_item
                        END
                    )
                    FROM jsonb_array_elements(""Quota""->'CategoryQuotas') AS category_item
                )
            ),
            ""UpdatedAt"" = {updatedAt}
            WHERE ""SubscriptionId"" = {subscriptionId}");

                if (rowsAffected == 0)
                {
                    _logger.LogWarning("No rows updated when recording usage for subscription {Id}", subscriptionId);
                    return false;
                }

                // Record usage in actor state
                categoryUsage.AdsUsed += amount;
                data.Quota.AdsUsed += amount;

                // Update actor state
                await FastSetDataAsync(data, cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                // Clear cache after update
                ClearCache();

                _logger.LogInformation("Free ads usage recorded successfully for subscription {Id}: category={Category}/{L1}/{L2}, amount={Amount}, remaining={Remaining}",
                    data.Id, category, l1Category, l2Category, amount, categoryUsage.AdsAllowed - categoryUsage.AdsUsed);

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error recording free ads usage for subscription {Id}", this.Id.GetId());
                return false;
            }
        }                          /// </summary>
        public async Task<List<FreeAdsCategorySummary>> GetFreeAdsUsageSummaryAsync(CancellationToken cancellationToken = default)
        {
            // ALWAYS sync from database to get latest data
            var data = await SyncFromDatabaseAsync(force: true, cancellationToken);
            if (data == null || data.ProductType != ProductType.FREE)
            {
                return new List<FreeAdsCategorySummary>();
            }

            return data.Quota.CategoryQuotas.Select(c => new FreeAdsCategorySummary
            {
                CategoryPath = GetCategoryPath(c.Category, c.L1Category, c.L2Category),
                FreeAdsAllowed = c.AdsAllowed,
                FreeAdsUsed = c.AdsUsed,
                FreeAdsRemaining = Math.Max(0, c.AdsAllowed - c.AdsUsed),
                UsagePercentage = c.AdsAllowed > 0 ? (double)c.AdsUsed / c.AdsAllowed * 100 : 0
            }).ToList();
        }

        /// <summary>
        /// Get remaining FREE ads quota for specific category
        /// </summary>
        public async Task<int> GetRemainingFreeAdsQuotaAsync(string category, string? l1Category, string? l2Category, CancellationToken cancellationToken = default)
        {
            var data = await SyncFromDatabaseAsync(force: true, cancellationToken);
            if (data == null || data.ProductType != ProductType.FREE)
            {
                return 0;
            }

            var categoryUsage = data.Quota.CategoryQuotas.FirstOrDefault(c =>
                c.Category == category &&
                c.L1Category == l1Category &&
                c.L2Category == l2Category);

            return categoryUsage != null ? Math.Max(0, categoryUsage.AdsAllowed - categoryUsage.AdsUsed) : 0;
        }

        private string GetCategoryPath(string category, string? l1Category, string? l2Category)
        {
            if (!string.IsNullOrEmpty(l2Category))
                return $"{category} > {l1Category} > {l2Category}";
            if (!string.IsNullOrEmpty(l1Category))
                return $"{category} > {l1Category}";
            return category;
        }

        #endregion

        #region Enhanced CreateSubscription for FREE Products

        /// <summary>
        /// Enhanced CreateSubscriptionAsync that properly handles FREE products with category quotas
        /// </summary>
        public async Task<bool> CreateFreeAdsSubscriptionAsync(V2SubscriptionPurchaseRequestDto request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            _logger.LogInformation("Creating FREE ads subscription for user {UserId} with product {ProductCode}",
                request.UserId, request.ProductCode);

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<QLSubscriptionContext>();
            var productService = scope.ServiceProvider.GetRequiredService<IProductService>();

            using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var subscriptionId = Guid.Parse(this.Id.GetId());

                // Validate product
                var product = await context.Products
                    .FirstOrDefaultAsync(p => p.ProductCode == request.ProductCode && p.IsActive, cancellationToken);

                if (product == null)
                    throw new InvalidOperationException($"Product with code {request.ProductCode} not found or inactive");

                if (product.ProductType != ProductType.FREE)
                    throw new InvalidOperationException($"Product {request.ProductCode} is not a FREE product type");

                // Build FREE ads subscription quota
                var freeAdsQuota = BuildFreeAdsQuotaFromProduct(product);

                // Create database subscription
                var dbSubscription = new Subscription
                {
                    SubscriptionId = subscriptionId,
                    ProductCode = product.ProductCode,
                    ProductName = product.ProductName,
                    ProductType = ProductType.FREE,
                    UserId = request.UserId,
                    AdId = request.AdId,
                    CompanyId = request.CompanyId,
                    PaymentId = request.PaymentId,
                    Vertical = product.Vertical,
                    SubVertical = product.SubVertical,
                    Quota = freeAdsQuota,
                    StartDate = DateTime.UtcNow,
                    EndDate = product.Constraints?.Duration.HasValue == true
                        ? DateTime.UtcNow.Add(product.Constraints.Duration.Value)
                        : DateTime.UtcNow.AddYears(1),
                    Status = SubscriptionStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.Subscriptions.Add(dbSubscription);
                await context.SaveChangesAsync(cancellationToken);

                // Create actor state
                var v2Dto = new V2SubscriptionDto
                {
                    Id = subscriptionId,
                    ProductCode = product.ProductCode,
                    ProductName = product.ProductName,
                    ProductType = ProductType.FREE,
                    UserId = request.UserId,
                    CompanyId = request.CompanyId,
                    PaymentId = request.PaymentId,
                    Vertical = product.Vertical,
                    SubVertical = product.SubVertical,
                    Price = 0, 
                    Currency = product.Currency,
                    Quota = freeAdsQuota,
                    StartDate = dbSubscription.StartDate,
                    EndDate = dbSubscription.EndDate,
                    StatusId = SubscriptionStatus.Active,
                    lastUpdated = DateTime.UtcNow,
                    Version = "V2"
                };

                await FastSetDataAsync(v2Dto, cancellationToken);

                // Commit transaction
                await transaction.CommitAsync(cancellationToken);

                // Clear cache to ensure fresh reads
                ClearCache();

                _logger.LogInformation("FREE ads subscription created successfully: {Id} for user: {UserId} with {CategoryCount} category quotas",
                    subscriptionId, request.UserId, freeAdsQuota.CategoryQuotas.Count);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to create FREE ads subscription for user {UserId}", request.UserId);
                throw;
            }
        }

        /// <summary>
        /// Build SubscriptionQuota specifically for FREE ads products
        /// </summary>
        private SubscriptionQuota BuildFreeAdsQuotaFromProduct(Product product)
        {
            var constraints = product.Constraints ?? new ProductConstraints();

            var quota = new SubscriptionQuota
            {
                Vertical = product.Vertical.ToString(),
                Scope = constraints.Scope ?? "Category-Based-Free",

                // Global quotas - sum of all category quotas for FREE products
                TotalAdsAllowed = constraints.CategoryQuotas?.Sum(c => c.AdsBudget) ?? 0,
                TotalPromotionsAllowed = 0, // No promotions for FREE
                TotalFeaturesAllowed = 0, // No features for FREE
                DailyRefreshesAllowed = 0, // No refreshes for FREE
                RefreshesPerAdAllowed = 0,
                SocialMediaPostsAllowed = 0,

                // Permissions for FREE products
                CanPublishAds = true, // Free to publish
                CanPromoteAds = false, // Must pay to promote
                CanFeatureAds = false, // Must pay to feature
                CanRefreshAds = false, // No refresh for FREE
                CanPostSocialMedia = false,

                RefreshInterval = "Not Available", // No refresh for FREE
                RefreshIntervalHours = 0
            };

            // Add category-specific quotas for FREE products
            if (constraints.CategoryQuotas != null)
            {
                foreach (var categoryQuota in constraints.CategoryQuotas)
                {
                    quota.CategoryQuotas.Add(new CategoryQuotaUsage
                    {
                        Category = categoryQuota.Category,
                        L1Category = categoryQuota.L1Category,
                        L2Category = categoryQuota.L2Category,
                        AdsAllowed = categoryQuota.AdsBudget,
                        AdsUsed = 0
                    });
                }
            }

            return quota;
        }
        public async Task<int> RefundFreeAdsUsageAsync(string category, string? l1Category, string? l2Category, int amount, CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<QLSubscriptionContext>();

            using var tx = await context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var data = await SyncFromDatabaseAsync(force: true, cancellationToken);
                if (data == null || data.ProductType != ProductType.FREE ||
                    data.StatusId != SubscriptionStatus.Active || data.EndDate <= DateTime.UtcNow)
                    return 0;

                var cat = data.Quota.CategoryQuotas.FirstOrDefault(c =>
                    c.Category == category &&
                    c.L1Category == l1Category &&
                    c.L2Category == l2Category);

                if (cat == null || cat.AdsUsed <= 0) return 0;

                var amountToRefund = Math.Max(0, Math.Min(amount, cat.AdsUsed));
                if (amountToRefund == 0) return 0;

                var subscriptionId = data.Id;
                var l1 = l1Category ?? "";
                var l2 = l2Category ?? "";
                var now = DateTime.UtcNow;

                var rows = await context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE ""Subscriptions""
                SET ""Quota"" = jsonb_set(
                    jsonb_set(""Quota"", '{{""AdsUsed""}}',
                        GREATEST(0, COALESCE((""Quota""->>'AdsUsed')::int, 0) - {amountToRefund})::text::jsonb
                    ),
                    '{{""CategoryQuotas""}}',
                    (
                        SELECT jsonb_agg(
                            CASE
                                WHEN category_item->>'Category' = {category}
                                 AND COALESCE(category_item->>'L1Category','') = COALESCE({l1},'')
                                 AND COALESCE(category_item->>'L2Category','') = COALESCE({l2},'')
                                THEN jsonb_set(
                                    category_item,
                                    '{{""AdsUsed""}}',
                                    GREATEST(0, (COALESCE(category_item->>'AdsUsed','0')::int - {amountToRefund}))::text::jsonb
                                )
                                ELSE category_item
                            END
                        )
                        FROM jsonb_array_elements(""Quota""->'CategoryQuotas') AS category_item
                    )
                ),
                ""UpdatedAt"" = {now}
                WHERE ""SubscriptionId"" = {subscriptionId}");

                if (rows == 0) return 0;

                cat.AdsUsed -= amountToRefund;
                data.Quota.AdsUsed = Math.Max(0, data.Quota.AdsUsed - amountToRefund);
                await FastSetDataAsync(data, cancellationToken);
                await tx.CommitAsync(cancellationToken);
                ClearCache();

                return amountToRefund;
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }
        }
        #endregion
    }
}
