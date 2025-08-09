using Dapr.Actors.Runtime;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s.Subscription;
using QLN.Common.Infrastructure.IService.IProductService;
using QLN.Common.Infrastructure.Model;
using System.Collections.Concurrent;

namespace QLN.Subscriptions.Actor.ActorClass
{
    public class V2SubscriptionActor : Dapr.Actors.Runtime.Actor, IV2SubscriptionActor
    {
        private const string V2StateKey = "v2-subscription-data";
        private readonly ILogger<V2SubscriptionActor> _logger;

        private static readonly ConcurrentDictionary<string, V2SubscriptionDto> _v2MemoryCache = new();
        private static volatile bool _v2StateStoreUnstable = false;
        private static DateTime _v2LastStateStoreFailure = DateTime.MinValue;
        private static readonly TimeSpan _v2CircuitBreakDuration = TimeSpan.FromMinutes(1);
        private static readonly SemaphoreSlim _v2StateStoreThrottle = new(5, 5);

        public V2SubscriptionActor(ActorHost host, ILogger<V2SubscriptionActor> logger) : base(host)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> FastSetDataAsync(V2SubscriptionDto data, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(data);
            var actorKey = Id.ToString();

            data.lastUpdated = DateTime.UtcNow;
            _v2MemoryCache[actorKey] = data;

            if (_v2StateStoreUnstable && DateTime.UtcNow - _v2LastStateStoreFailure < _v2CircuitBreakDuration)
                return true;

            if (await _v2StateStoreThrottle.WaitAsync(TimeSpan.FromMilliseconds(500), cancellationToken))
            {
                try
                {
                    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);

                    await StateManager.SetStateAsync(V2StateKey, data, linkedCts.Token);
                    await StateManager.SaveStateAsync(linkedCts.Token);

                    _v2StateStoreUnstable = false;
                }
                catch (Exception)
                {
                    _v2StateStoreUnstable = true;
                    _v2LastStateStoreFailure = DateTime.UtcNow;
                }
                finally
                {
                    _v2StateStoreThrottle.Release();
                }
            }

            return true;
        }

        public Task<bool> SetDataAsync(V2SubscriptionDto data, CancellationToken cancellationToken = default)
            => FastSetDataAsync(data, cancellationToken);

        public async Task<V2SubscriptionDto?> GetDataAsync(CancellationToken cancellationToken = default)
        {
            var actorKey = Id.ToString();

            if (_v2MemoryCache.TryGetValue(actorKey, out var cachedData))
                return cachedData;

            if (_v2StateStoreUnstable && DateTime.UtcNow - _v2LastStateStoreFailure < _v2CircuitBreakDuration)
                return null;

            if (await _v2StateStoreThrottle.WaitAsync(TimeSpan.FromMilliseconds(500), cancellationToken))
            {
                try
                {
                    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);

                    var conditionalValue = await StateManager.TryGetStateAsync<V2SubscriptionDto>(V2StateKey, linkedCts.Token);
                    if (!conditionalValue.HasValue) return null;

                    var data = conditionalValue.Value;
                    if (data != null) _v2MemoryCache[actorKey] = data;

                    _v2StateStoreUnstable = false;
                    return data;
                }
                catch (Exception)
                {
                    _v2StateStoreUnstable = true;
                    _v2LastStateStoreFailure = DateTime.UtcNow;
                    return null;
                }
                finally
                {
                    _v2StateStoreThrottle.Release();
                }
            }

            return null;
        }

        public async Task<bool> ValidateUsageAsync(string quotaType, decimal requestedAmount, CancellationToken cancellationToken = default)
        {
            var data = await GetDataAsync(cancellationToken);
            if (data == null) return false;

            if (data.StatusId != V2Status.Active || data.EndDate <= DateTime.UtcNow)
                return false;

            var qty = (int)Math.Ceiling(requestedAmount);
            var action = MapQuotaTypeToAction(quotaType);
            var result = data.Quota.ValidateAction(action, qty);
            return result.IsValid;
        }

        public async Task<bool> RecordUsageAsync(string quotaType, decimal amount, CancellationToken cancellationToken = default)
        {
            var data = await GetDataAsync(cancellationToken);
            if (data == null) return false;

            if (data.StatusId != V2Status.Active || data.EndDate <= DateTime.UtcNow)
                return false;

            var qty = (int)Math.Ceiling(amount);
            var action = MapQuotaTypeToAction(quotaType);

            var success = data.Quota.RecordUsage(action, qty);
            if (!success) return false;

            data.lastUpdated = DateTime.UtcNow;
            return await FastSetDataAsync(data, cancellationToken);
        }

        public async Task<bool> IsActiveAsync(CancellationToken cancellationToken = default)
        {
            var data = await GetDataAsync(cancellationToken);
            return data != null && data.StatusId == V2Status.Active && data.EndDate > DateTime.UtcNow;
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

        protected override Task OnActivateAsync() => base.OnActivateAsync();
        protected override Task OnDeactivateAsync() => base.OnDeactivateAsync();
    }
}
