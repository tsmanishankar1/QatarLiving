using Dapr.Actors.Runtime;
using Dapr.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s.Payments;
using QLN.Common.DTO_s.Subscription;
using QLN.Common.Infrastructure.IService.IProductService;
using QLN.Common.Infrastructure.Model;
using QLN.Common.Infrastructure.QLDbContext;
using QLN.Common.Infrastructure.Subscriptions;
using System.Collections.Concurrent;

namespace QLN.Subscriptions.Actor.ActorClass
{
    /// <summary>
    /// V2 User Addon Actor - Always syncs from database on all read operations
    /// Ensures manual database changes are immediately reflected in all API responses
    /// </summary>
    public class V2UserAddonActor : Dapr.Actors.Runtime.Actor, IV2UserAddonActor, IRemindable
    {
        private const string StateKey = "v2-addon-data";
        private const string ReminderName = "CheckAddonExpiryReminder";
        private const string PubSubName = "pubsub";

        private readonly ILogger<V2UserAddonActor> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        // Memory cache for actor state - only used for brief performance optimization
        private static readonly ConcurrentDictionary<string, V2UserAddonDto> _memoryCache = new();

        public V2UserAddonActor(
            ActorHost host,
            ILogger<V2UserAddonActor> logger,
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

            _logger.LogInformation("V2UserAddonActor {ActorId} activated with expiry reminder", this.Id.GetId());
        }

        protected override async Task OnDeactivateAsync()
        {
            // Clean up memory cache entry
            var key = this.Id.GetId();
            _memoryCache.TryRemove(key, out _);

            await base.OnDeactivateAsync();
            _logger.LogInformation("V2UserAddonActor {ActorId} deactivated", this.Id.GetId());
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
                    _logger.LogInformation("Addon {Id} expired by reminder, marking as expired", data.Id);
                    await MarkAsExpiredAsync(CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in expiry reminder for addon {Id}", this.Id.GetId());
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

        public async Task<bool> CreateAddonAsync(V2UserAddonPurchaseRequestDto request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            _logger.LogInformation("Creating addon for user {UserId} with product {ProductCode}",
                request.UserId, request.ProductCode);

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<QLSubscriptionContext>();
            var daprClient = scope.ServiceProvider.GetRequiredService<DaprClient>();

            using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var addonId = Guid.Parse(this.Id.GetId());

                // Validate product
                var product = await context.Products
                    .FirstOrDefaultAsync(p => p.ProductCode == request.ProductCode && p.IsActive, cancellationToken);

                if (product == null)
                    throw new InvalidOperationException($"Addon product with code {request.ProductCode} not found or inactive");

                var addonTypes = new[] { ProductType.ADDON_COMBO, ProductType.ADDON_FEATURE, ProductType.ADDON_REFRESH, ProductType.ADDON_PROMOTE };
                if (!addonTypes.Contains(product.ProductType))
                    throw new InvalidOperationException($"Product {request.ProductCode} is not an addon product");

                // Create database addon
                var dbAddon = new UserAddOn
                {
                    UserAddOnId = addonId,
                    ProductCode = product.ProductCode,
                    ProductName = product.ProductName,
                    UserId = request.UserId,
                    CompanyId = request.CompanyId,
                    SubscriptionId = request.SubscriptionId,
                    PaymentId = request.PaymentId,
                    Vertical = product.Vertical,
                    SubVertical = product.SubVertical,
                    Quota = BuildAddonQuotaFromProduct(product),
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.Add(GetDurationFromProduct(product)),
                    Status = SubscriptionStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.UserAddOns.Add(dbAddon);
                await context.SaveChangesAsync(cancellationToken);

                // Create actor state
                var v2AddonDto = new V2UserAddonDto
                {
                    Id = addonId,
                    ProductCode = product.ProductCode,
                    ProductName = product.ProductName,
                    UserId = request.UserId,
                    CompanyId = request.CompanyId,
                    SubscriptionId = request.SubscriptionId,
                    PaymentId = request.PaymentId,
                    Vertical = product.Vertical,
                    SubVertical = product.SubVertical,
                    Price = product.Price,
                    Currency = product.Currency,
                    Quota = dbAddon.Quota,
                    StartDate = dbAddon.StartDate,
                    EndDate = dbAddon.EndDate,
                    StatusId = SubscriptionStatus.Active,
                    lastUpdated = DateTime.UtcNow,
                    Version = "V2"
                };

                await FastSetDataAsync(v2AddonDto, cancellationToken);

                // Commit transaction before publishing events
                await transaction.CommitAsync(cancellationToken);

                // Clear cache to ensure fresh reads
                ClearCache();

                _logger.LogInformation("V2 Addon created successfully: {Id} for user: {UserId}",
                    addonId, request.UserId);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to create V2 addon for user {UserId}", request.UserId);
                throw;
            }
        }

        public async Task<bool> FastSetDataAsync(V2UserAddonDto data, CancellationToken cancellationToken = default)
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

        public Task<bool> SetDataAsync(V2UserAddonDto data, CancellationToken cancellationToken = default)
            => FastSetDataAsync(data, cancellationToken);

        /// <summary>
        /// ALWAYS syncs from database to ensure manual changes are reflected
        /// This method is called by all GET endpoints to ensure fresh data
        /// </summary>
        public async Task<V2UserAddonDto?> GetDataAsync(CancellationToken cancellationToken = default)
        {
            // ALWAYS force sync from database for all read operations
            return await SyncFromDatabaseAsync(force: true, cancellationToken);
        }

        /// <summary>
        /// ALWAYS syncs from database before validation to ensure latest status
        /// </summary>
        public async Task<bool> ValidateUsageAsync(string quotaType, int requestedAmount, CancellationToken cancellationToken = default)
        {
            // ALWAYS sync from database first to catch manual status changes
            var data = await SyncFromDatabaseAsync(force: true, cancellationToken);
            if (data == null)
            {
                _logger.LogWarning("Cannot validate usage: addon data not found for {Id}", this.Id.GetId());
                return false;
            }

            if (data.StatusId != SubscriptionStatus.Active || data.EndDate <= DateTime.UtcNow)
            {
                _logger.LogWarning("Cannot validate usage: addon {Id} is not active (Status: {Status}) or expired (EndDate: {EndDate})",
                    data.Id, data.StatusId, data.EndDate);
                return false;
            }

            var action = MapQuotaTypeToAction(quotaType);
            var validationResult = data.Quota.ValidateAction(action, requestedAmount);

            _logger.LogInformation("Usage validation for addon {Id}: quotaType={QuotaType}, amount={Amount}, valid={IsValid}",
                data.Id, quotaType, requestedAmount, validationResult.IsValid);

            return validationResult.IsValid;
        }

        public async Task<bool> RecordUsageAsync(string quotaType, int amount, CancellationToken cancellationToken = default)
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
                    _logger.LogWarning("Cannot record usage: addon data not found for {Id}", this.Id.GetId());
                    return false;
                }

                if (data.StatusId != SubscriptionStatus.Active || data.EndDate <= DateTime.UtcNow)
                {
                    _logger.LogWarning("Cannot record usage: addon {Id} is not active (Status: {Status}) or expired (EndDate: {EndDate})",
                        data.Id, data.StatusId, data.EndDate);
                    return false;
                }

                var action = MapQuotaTypeToAction(quotaType);

                // Record usage in actor state
                var success = data.Quota.RecordUsage(action, amount);
                if (!success)
                {
                    _logger.LogWarning("Failed to record usage in actor state for addon {Id}", data.Id);
                    return false;
                }

                // Update database
                var addonId = Guid.Parse(this.Id.GetId());
                var dbAddon = await context.UserAddOns
                    .FirstOrDefaultAsync(a => a.UserAddOnId == addonId, cancellationToken);

                if (dbAddon != null)
                {
                    dbAddon.Quota.RecordUsage(action, amount);
                    dbAddon.UpdatedAt = DateTime.UtcNow;
                    await context.SaveChangesAsync(cancellationToken);
                }

                // Update actor state
                await FastSetDataAsync(data, cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                // Clear cache after update
                ClearCache();

                _logger.LogInformation("Usage recorded successfully for addon {Id}: quotaType={QuotaType}, amount={Amount}",
                    data.Id, quotaType, amount);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error recording usage for addon {Id}", this.Id.GetId());
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
            _logger.LogDebug("Addon {Id} active status: {IsActive} (Status: {Status}, EndDate: {EndDate})",
                data.Id, isActive, data.StatusId, data.EndDate);
            return isActive;
        }

        public async Task<bool> MarkAsExpiredAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<QLSubscriptionContext>();
            var daprClient = scope.ServiceProvider.GetRequiredService<DaprClient>();

            using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var addonId = Guid.Parse(this.Id.GetId());

                var dbAddon = await context.UserAddOns
                    .FirstOrDefaultAsync(a => a.UserAddOnId == addonId, cancellationToken);

                if (dbAddon == null)
                {
                    _logger.LogWarning("Cannot mark as expired: addon {Id} not found", addonId);
                    return false;
                }

                // Update database
                dbAddon.Status = SubscriptionStatus.Expired;
                dbAddon.UpdatedAt = DateTime.UtcNow;
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

                    // Publish addon expired event
                    await PublishAddonExpiredEventAsync(existingData, daprClient, cancellationToken);
                }
                else
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                _logger.LogInformation("Addon {Id} marked as expired", addonId);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error marking addon {Id} as expired", this.Id.GetId());
                return false;
            }
        }

        public async Task<bool> CancelAddonAsync(string userId, CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<QLSubscriptionContext>();
            var daprClient = scope.ServiceProvider.GetRequiredService<DaprClient>();

            using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var addonId = Guid.Parse(this.Id.GetId());

                var dbAddon = await context.UserAddOns
                    .FirstOrDefaultAsync(a => a.UserAddOnId == addonId && a.UserId == userId, cancellationToken);

                if (dbAddon == null)
                {
                    _logger.LogWarning("Cannot cancel: addon {Id} not found for user {UserId}", addonId, userId);
                    return false;
                }

                // Update database
                dbAddon.Status = SubscriptionStatus.Cancelled;
                dbAddon.UpdatedAt = DateTime.UtcNow;
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

                    // Publish addon cancelled event
                    await PublishAddonCancelledEventAsync(existingData, daprClient, cancellationToken);
                }
                else
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                _logger.LogInformation("V2 Addon {Id} cancelled for user {UserId}", addonId, userId);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error cancelling V2 addon {Id} for user {UserId}", this.Id.GetId(), userId);
                throw;
            }
        }

        public async Task<bool> ExtendAddonAsync(TimeSpan additionalDuration, CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<QLSubscriptionContext>();

            using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var addonId = Guid.Parse(this.Id.GetId());

                var dbAddon = await context.UserAddOns
                    .FirstOrDefaultAsync(a => a.UserAddOnId == addonId, cancellationToken);

                if (dbAddon == null)
                {
                    _logger.LogWarning("Cannot extend: addon {Id} not found", addonId);
                    return false;
                }

                // Update database
                dbAddon.EndDate = dbAddon.EndDate.Add(additionalDuration);
                dbAddon.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync(cancellationToken);

                // Update actor state with fresh data
                var actorData = await SyncFromDatabaseAsync(force: true, cancellationToken);
                if (actorData != null)
                {
                    actorData.EndDate = dbAddon.EndDate;
                    actorData.lastUpdated = DateTime.UtcNow;
                    await FastSetDataAsync(actorData, cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);

                // Clear cache after update
                ClearCache();

                _logger.LogInformation("Addon {Id} extended by {Duration}", addonId, additionalDuration);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error extending addon {Id}", this.Id.GetId());
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
                var addonId = Guid.Parse(this.Id.GetId());

                var dbAddon = await context.UserAddOns
                    .FirstOrDefaultAsync(a => a.UserAddOnId == addonId, cancellationToken);

                if (dbAddon == null)
                {
                    _logger.LogWarning("Cannot refill quota: addon {Id} not found", addonId);
                    return false;
                }

                // Update database quota
                var qty = (int)Math.Ceiling(amount);
                switch ((quotaType ?? string.Empty).ToLower())
                {
                    case V2QuotaTypes.AdsBudget:
                        dbAddon.Quota.TotalAdsAllowed += qty;
                        break;
                    case V2QuotaTypes.PromoteBudget:
                        dbAddon.Quota.TotalPromotionsAllowed += qty;
                        break;
                    case V2QuotaTypes.FeatureBudget:
                        dbAddon.Quota.TotalFeaturesAllowed += qty;
                        break;
                    case V2QuotaTypes.RefreshBudget:
                        dbAddon.Quota.DailyRefreshesAllowed += qty;
                        break;
                    case V2QuotaTypes.SocialMediaPosts:
                        dbAddon.Quota.SocialMediaPostsAllowed += qty;
                        break;
                    default:
                        _logger.LogWarning("Unknown quotaType '{QuotaType}' in refill for addon {Id}", quotaType, addonId);
                        return false;
                }

                dbAddon.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync(cancellationToken);

                // Update actor state with fresh data
                var actorData = await SyncFromDatabaseAsync(force: true, cancellationToken);
                if (actorData != null)
                {
                    actorData.Quota = dbAddon.Quota;
                    actorData.lastUpdated = DateTime.UtcNow;
                    await FastSetDataAsync(actorData, cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);

                // Clear cache after update
                ClearCache();

                _logger.LogInformation("Addon {Id} quota {QuotaType} refilled by {Amount}", addonId, quotaType, amount);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error refilling addon {Id} quota", this.Id.GetId());
                return false;
            }
        }

        public async Task<bool> UpdateStatusAsync(SubscriptionStatus newStatus, CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<QLSubscriptionContext>();

            using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var addonId = Guid.Parse(this.Id.GetId());

                var dbAddon = await context.UserAddOns
                    .FirstOrDefaultAsync(a => a.UserAddOnId == addonId, cancellationToken);

                if (dbAddon == null)
                {
                    _logger.LogWarning("Cannot update status: addon {Id} not found", addonId);
                    return false;
                }

                // Update database
                dbAddon.Status = newStatus;
                dbAddon.UpdatedAt = DateTime.UtcNow;
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

                _logger.LogInformation("Addon {Id} status updated to {Status}", addonId, newStatus);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error updating addon {Id} status", this.Id.GetId());
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
                var addonId = Guid.Parse(this.Id.GetId());

                var dbAddon = await context.UserAddOns
                    .FirstOrDefaultAsync(a => a.UserAddOnId == addonId, cancellationToken);

                if (dbAddon == null)
                {
                    _logger.LogWarning("Cannot update end date: addon {Id} not found", addonId);
                    return false;
                }

                // Update database
                dbAddon.EndDate = newEndDate;
                dbAddon.UpdatedAt = DateTime.UtcNow;
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

                _logger.LogInformation("Addon {Id} end date updated to {EndDate}", addonId, newEndDate);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error updating addon {Id} end date", this.Id.GetId());
                return false;
            }
        }

        #endregion

        #region Cache Management

        private void ClearCache()
        {
            var key = this.Id.GetId();
            _memoryCache.TryRemove(key, out _);
            _logger.LogDebug("Cleared cache for addon {Id}", key);
        }

        #endregion

        #region Event Publishing

        private async Task PublishAddonCancelledEventAsync(V2UserAddonDto data, DaprClient daprClient, CancellationToken cancellationToken)
        {
            try
            {
                var topic = GetCancelledTopicForVertical(data.Vertical);
                var eventData = new V2AddonCancelledEventDto
                {
                    AddonId = data.Id,
                    ProductCode = data.ProductCode,
                    UserId = data.UserId ?? string.Empty,
                    SubscriptionId = data.SubscriptionId,
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
                _logger.LogInformation("Published {Topic} for addon {Id} (Vertical={Vertical}, SubVertical={SubVertical})",
                    topic, data.Id, data.Vertical, data.SubVertical?.ToString() ?? "null");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish addon cancelled event for {Id}", data.Id);
            }
        }

        private async Task PublishAddonExpiredEventAsync(V2UserAddonDto data, DaprClient daprClient, CancellationToken cancellationToken)
        {
            try
            {
                var topic = GetExpiredTopicForVertical(data.Vertical);
                var eventData = new V2AddonExpiredEventDto
                {
                    AddonId = data.Id,
                    ProductCode = data.ProductCode,
                    UserId = data.UserId ?? string.Empty,
                    SubscriptionId = data.SubscriptionId,
                    Vertical = data.Vertical,
                    SubVertical = data.SubVertical,
                    ExpiredAt = DateTime.UtcNow,
                    EventId = Guid.NewGuid(),
                    Version = "V2",
                    Metadata = new System.Collections.Generic.Dictionary<string, object>
                    {
                        ["actualEndDate"] = data.EndDate,
                        ["parentSubscriptionId"] = data.SubscriptionId,
                        ["quotaUsage"] = new
                        {
                            adsUsed = data.Quota.AdsUsed,
                            promotionsUsed = data.Quota.PromotionsUsed,
                            featuresUsed = data.Quota.FeaturesUsed
                        }
                    }
                };

                await daprClient.PublishEventAsync(PubSubName, topic, eventData, cancellationToken);
                _logger.LogInformation("Published {Topic} for addon {Id} (Vertical={Vertical}, SubVertical={SubVertical})",
                    topic, data.Id, data.Vertical, data.SubVertical?.ToString() ?? "null");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish addon expired event for {Id}", data.Id);
            }
        }

        private static string GetCancelledTopicForVertical(Vertical vertical) => vertical switch
        {
            Vertical.Classifieds => "addon.cancelled.classifieds",
            Vertical.Properties => "addon.cancelled.properties",
            Vertical.Services => "addon.cancelled.services",
            Vertical.Rewards => "addon.cancelled.rewards",
            _ => "addon.cancelled.unknown"
        };

        private static string GetExpiredTopicForVertical(Vertical vertical) => vertical switch
        {
            Vertical.Classifieds => "addon.expired.classifieds",
            Vertical.Properties => "addon.expired.properties",
            Vertical.Services => "addon.expired.services",
            Vertical.Rewards => "addon.expired.rewards",
            _ => "addon.expired.unknown"
        };

        #endregion

        #region Database Sync and Helper Methods

        /// <summary>
        /// Syncs actor state from DB - database is always the source of truth
        /// When force=true, always reads from database to catch manual changes
        /// </summary>
        private async Task<V2UserAddonDto?> SyncFromDatabaseAsync(bool force, CancellationToken cancellationToken)
        {
            var key = this.Id.GetId();

            // For performance, check cache first only if not forcing
            if (!force && _memoryCache.TryGetValue(key, out var cached))
            {
                _logger.LogDebug("Using cached data for addon {Id}", key);
                return cached;
            }

            _logger.LogDebug("Syncing addon {Id} from database (force: {Force})", key, force);

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<QLSubscriptionContext>();

            if (!Guid.TryParse(key, out var addonId))
            {
                _logger.LogWarning("Invalid addon ID format: {Id}", key);
                return null;
            }

            var dbAddon = await context.UserAddOns
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.UserAddOnId == addonId, cancellationToken);

            if (dbAddon == null)
            {
                _logger.LogWarning("Addon {Id} not found in database during sync", key);
                return await TryReadFromActorStateAsync(cancellationToken);
            }

            var mappedFromDb = MapDbToV2Dto(dbAddon);

            // Update actor state and cache with latest database data
            await FastSetDataAsync(mappedFromDb, cancellationToken);
            _logger.LogDebug("Synced addon {Id} from database - Status: {Status}, EndDate: {EndDate}",
                addonId, mappedFromDb.StatusId, mappedFromDb.EndDate);

            return mappedFromDb;
        }

        private async Task<V2UserAddonDto?> TryReadFromActorStateAsync(CancellationToken cancellationToken)
        {
            var key = this.Id.GetId();

            // Check memory cache first
            if (_memoryCache.TryGetValue(key, out var cached))
                return cached;

            // Read from actor state store
            var stateResult = await StateManager.TryGetStateAsync<V2UserAddonDto>(StateKey, cancellationToken);
            if (stateResult.HasValue && stateResult.Value != null)
            {
                _memoryCache[key] = stateResult.Value;
                return stateResult.Value;
            }

            return null;
        }

        private SubscriptionQuota BuildAddonQuotaFromProduct(Product product)
        {
            var constraints = product.Constraints ?? new ProductConstraints();

            var quota = new SubscriptionQuota
            {
                Vertical = product.Vertical.ToString(),
                Scope = constraints.Scope ?? "All",
                CanPublishAds = false,
                CanPromoteAds = false,
                CanFeatureAds = false,
                CanRefreshAds = false,
                CanPostSocialMedia = false,
                RefreshInterval = "Every 72 Hours",
                RefreshIntervalHours = 72
            };

            // Configure addon-specific quota based on product constraints
            if (constraints.PayToPublish == true)
            {
                quota.CanPublishAds = true;
                quota.TotalAdsAllowed += 1;
            }

            if (constraints.PayToPromote == true)
            {
                quota.CanPromoteAds = true;
                quota.TotalPromotionsAllowed += 1;
            }

            if (constraints.PayToFeature == true)
            {
                quota.CanFeatureAds = true;
                quota.TotalFeaturesAllowed += 1;
            }

            if ((constraints.RefreshBudgetPerDay ?? 0) > 0)
            {
                quota.CanRefreshAds = true;
                quota.DailyRefreshesAllowed = constraints.RefreshBudgetPerDay.Value;
            }

            // Add budget-based quotas
            if (constraints.PromotedBudget.HasValue)
            {
                quota.CanPromoteAds = true;
                quota.TotalPromotionsAllowed += constraints.PromotedBudget.Value;
            }

            if (constraints.FeaturedBudget.HasValue)
            {
                quota.CanFeatureAds = true;
                quota.TotalFeaturesAllowed += constraints.FeaturedBudget.Value;
            }

            if (constraints.AdsBudget.HasValue)
            {
                quota.CanPublishAds = true;
                quota.TotalAdsAllowed += constraints.AdsBudget.Value;
            }

            if (constraints.RefreshBudgetPerAd.HasValue)
            {
                quota.CanRefreshAds = true;
                quota.RefreshesPerAdAllowed = Math.Max(quota.RefreshesPerAdAllowed, constraints.RefreshBudgetPerAd.Value);
            }

            return quota;
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
                ProductType.ADDON_FEATURE => TimeSpan.FromDays(30),
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

        private static V2UserAddonDto MapDbToV2Dto(UserAddOn dbAddon)
        {
            return new V2UserAddonDto
            {
                Id = dbAddon.UserAddOnId,
                ProductCode = dbAddon.ProductCode,
                ProductName = dbAddon.ProductName,
                UserId = dbAddon.UserId,
                CompanyId = dbAddon.CompanyId,
                SubscriptionId = dbAddon.SubscriptionId,
                PaymentId = dbAddon.PaymentId,
                Vertical = dbAddon.Vertical,
                SubVertical = dbAddon.SubVertical,
                Price = 0, // Default price, could be enhanced to store actual price
                Currency = "QAR",
                Quota = dbAddon.Quota,
                StartDate = dbAddon.StartDate,
                EndDate = dbAddon.EndDate,
                StatusId = dbAddon.Status,
                lastUpdated = dbAddon.UpdatedAt ?? dbAddon.CreatedAt,
                Version = "V2"
            };
        }

        #endregion
        public async Task<bool> AdminCancelAddonAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<QLSubscriptionContext>();
            var daprClient = scope.ServiceProvider.GetRequiredService<DaprClient>();

            using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var addonId = Guid.Parse(this.Id.GetId());

                var dbAddon = await context.UserAddOns
                    .FirstOrDefaultAsync(a => a.UserAddOnId == addonId, cancellationToken);

                if (dbAddon == null)
                {
                    _logger.LogWarning("Cannot cancel: addon {Id} not found", addonId);
                    return false;
                }

                // Update database (no user validation for admin)
                dbAddon.Status = SubscriptionStatus.Cancelled;
                dbAddon.UpdatedAt = DateTime.UtcNow;
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

                    // Publish addon cancelled event
                    await PublishAddonCancelledEventAsync(existingData, daprClient, cancellationToken);
                }
                else
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                _logger.LogInformation("V2 Addon {Id} cancelled by admin", addonId);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error admin cancelling V2 addon {Id}", this.Id.GetId());
                throw;
            }
        }
    }
}
