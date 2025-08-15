using Dapr.Actors.Runtime;
using Dapr.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s.Subscription;
using QLN.Common.Infrastructure.IService.IProductService;
using QLN.Common.Infrastructure.Model;
using QLN.Common.Infrastructure.QLDbContext;
using QLN.Common.Infrastructure.Subscriptions;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace QLN.Subscriptions.Actor.ActorClass
{
    public class V2SubscriptionActor : Dapr.Actors.Runtime.Actor, IV2SubscriptionActor, IRemindable
    {
        private const string StateKey = "v2-subscription-data";
        private const string ReminderName = "CheckExpiryReminder";
        private const string PubSubName = "pubsub";

        private readonly ILogger<V2SubscriptionActor> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        private static readonly ConcurrentDictionary<string, V2SubscriptionDto> _mem = new();

        public V2SubscriptionActor(
            ActorHost host,
            ILogger<V2SubscriptionActor> logger,
            IServiceScopeFactory scopeFactory
        ) : base(host)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        }

        #region Activation / Reminders

        protected override async Task OnActivateAsync()
        {
            await base.OnActivateAsync();
            await RegisterReminderAsync(
                ReminderName,
                null,
                GetDelayUntilMidnightUtc(),
                TimeSpan.FromDays(1));
        }

        public async Task ReceiveReminderAsync(string reminderName, byte[] state, TimeSpan dueTime, TimeSpan period)
        {
            if (!string.Equals(reminderName, ReminderName, StringComparison.Ordinal))
                return;

            try
            {
                var data = await SyncFromDatabaseAsync(force: true, CancellationToken.None);
                if (data == null) return;

                if (data.StatusId != V2Status.Expired && data.EndDate <= DateTime.UtcNow)
                {
                    _logger.LogInformation("Subscription {Id} expired by reminder.", data.Id);
                    await MarkExpiredAndPublishAsync(data, CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in expiry reminder for {Id}", this.Id.GetId());
            }
        }

        private static TimeSpan GetDelayUntilMidnightUtc()
        {
            var now = DateTime.UtcNow;
            var next = now.Date.AddDays(1);
            return next - now;
        }

        #endregion

        #region Public API

        public async Task<bool> FastSetDataAsync(V2SubscriptionDto data, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(data);
            data.lastUpdated = DateTime.UtcNow;

            var key = this.Id.GetId();
            _mem[key] = data;

            await StateManager.SetStateAsync(StateKey, data, cancellationToken);
            await StateManager.SaveStateAsync(cancellationToken);
            return true;
        }

        public Task<bool> SetDataAsync(V2SubscriptionDto data, CancellationToken cancellationToken = default)
            => FastSetDataAsync(data, cancellationToken);

        public async Task<V2SubscriptionDto?> GetDataAsync(CancellationToken cancellationToken = default)
        {
            var data = await SyncFromDatabaseAsync(force: false, cancellationToken);
            return data;
        }

        public async Task<bool> ValidateUsageAsync(string quotaType, decimal requestedAmount, CancellationToken cancellationToken = default)
        {
            var data = await SyncFromDatabaseAsync(force: false, cancellationToken);
            if (data == null) return false;

            if (data.StatusId != V2Status.Active || data.EndDate <= DateTime.UtcNow)
                return false;

            var qty = (int)Math.Ceiling(requestedAmount);
            var action = MapQuotaTypeToAction(quotaType);
            var res = data.Quota.ValidateAction(action, qty);
            return res.IsValid;
        }

        public async Task<bool> RecordUsageAsync(string quotaType, decimal amount, CancellationToken cancellationToken = default)
        {
            var data = await SyncFromDatabaseAsync(force: false, cancellationToken);
            if (data == null) return false;

            if (data.StatusId != V2Status.Active || data.EndDate <= DateTime.UtcNow)
                return false;

            var qty = (int)Math.Ceiling(amount);
            var action = MapQuotaTypeToAction(quotaType);
            Console.WriteLine("RecordusageAsync1:" );
            var ok = data.Quota.RecordUsage(action, qty);
            if (!ok) return false;

            return await FastSetDataAsync(data, cancellationToken);
        }

        public async Task<bool> IsActiveAsync(CancellationToken cancellationToken = default)
        {
            var data = await SyncFromDatabaseAsync(force: false, cancellationToken);
            if (data == null) return false;

            return data.StatusId == V2Status.Active && data.EndDate > DateTime.UtcNow;
        }

        #endregion

        #region Sync / Expiry Helpers

        /// <summary>
        /// Syncs actor state from DB (always the source of truth for admin/manual edits).
        /// If state is missing or DB is newer/changed, refreshes actor state and memory cache.
        /// </summary>
        private async Task<V2SubscriptionDto?> SyncFromDatabaseAsync(bool force, CancellationToken ct)
        {
            var key = this.Id.GetId();

            if (!force && _mem.TryGetValue(key, out var cached))
            {
                return cached;
            }

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<QLSubscriptionContext>();

            if (!Guid.TryParse(key, out var subscriptionId))
                return null;

            var dbSub = await db.Subscriptions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId, ct);

            if (dbSub == null)
            {
                _logger.LogWarning("Subscription {Id} not found in DB during sync.", key);
                return await TryReadFromStateAsync(ct);
            }

            var mapped = MapDbToV2Dto(dbSub);

            var state = await TryReadFromStateAsync(ct);
            if (force || state == null || !AreEquivalent(state, mapped))
            {
                await FastSetDataAsync(mapped, ct);
            }

            return mapped;
        }

        private async Task<V2SubscriptionDto?> TryReadFromStateAsync(CancellationToken ct)
        {
            var key = this.Id.GetId();

            if (_mem.TryGetValue(key, out var cached))
                return cached;

            var val = await StateManager.TryGetStateAsync<V2SubscriptionDto>(StateKey, ct);
            if (val.HasValue && val.Value != null)
            {
                _mem[key] = val.Value!;
                return val.Value!;
            }
            return null;
        }

        private static bool AreEquivalent(V2SubscriptionDto a, V2SubscriptionDto b)
        {
            if (a.Id != b.Id) return false;
            if (a.EndDate != b.EndDate) return false;
            if (a.StatusId != b.StatusId) return false;
            if (a.Vertical != b.Vertical) return false;
            if (a.SubVertical != b.SubVertical) return false;
            return true;
        }

        private async Task MarkExpiredAndPublishAsync(V2SubscriptionDto data, CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<QLSubscriptionContext>();
            var dapr = scope.ServiceProvider.GetRequiredService<DaprClient>();

            var sub = await db.Subscriptions.FirstOrDefaultAsync(s => s.SubscriptionId == data.Id, ct);
            if (sub != null)
            {
                sub.Status = SubscriptionStatus.Expired;
                sub.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
            }

            data.StatusId = V2Status.Expired;
            data.lastUpdated = DateTime.UtcNow;
            await FastSetDataAsync(data, ct);

            var topic = GetExpiredTopicForVertical(data.Vertical);
            var ev = new V2SubscriptionExpiredEventDto
            {
                SubscriptionId = data.Id,
                ProductCode = data.ProductCode,
                UserId = data.UserId ?? string.Empty,
                Vertical = data.Vertical,
                SubVertical = data.SubVertical,
                ExpiredAt = DateTime.UtcNow,
                EventId = Guid.NewGuid(),
                Version = "V2",
                Metadata = new System.Collections.Generic.Dictionary<string, object>()
            };

            await dapr.PublishEventAsync(PubSubName, topic, ev, ct);
            _logger.LogInformation("Published {Topic} for {Id} (Vertical={Vertical}, SubVertical={SubVertical}).",
                topic, data.Id, data.Vertical, data.SubVertical?.ToString() ?? "null");
        }

        private static string GetExpiredTopicForVertical(Vertical vertical) => vertical switch
        {
            Vertical.Classifieds => "subscription.expired.classifieds",
            Vertical.Properties => "subscription.expired.properties",
            Vertical.Services => "subscription.expired.services",
            Vertical.Rewards => "subscription.expired.rewards",
            _ => "subscription.expired.unknown"
        };

        #endregion

        #region Mapping / Utils

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

        private static V2SubscriptionDto MapDbToV2Dto(Subscription dbSub)
        {
            return new V2SubscriptionDto
            {
                Id = dbSub.SubscriptionId,
                ProductCode = dbSub.ProductCode,
                ProductName = dbSub.ProductName,
                UserId = dbSub.UserId,
                CompanyId = dbSub.CompanyId,
                PaymentId = dbSub.PaymentId,
                Vertical = dbSub.Vertical,
                SubVertical = dbSub.SubVertical, 
                Price = 0, 
                Currency = "QAR",
                Quota = dbSub.Quota,
                StartDate = dbSub.StartDate,
                EndDate = dbSub.EndDate,
                StatusId = dbSub.Status switch
                {
                    SubscriptionStatus.Active => V2Status.Active,
                    SubscriptionStatus.Expired => V2Status.Expired,
                    SubscriptionStatus.Cancelled => V2Status.Cancelled,
                    SubscriptionStatus.Suspended => V2Status.Suspended,
                    SubscriptionStatus.PaymentPending => V2Status.PaymentPending,
                    _ => V2Status.PaymentPending
                },
                lastUpdated = dbSub.UpdatedAt ?? dbSub.CreatedAt,
                Version = "V2"
            };
        }

        #endregion
    }
}
