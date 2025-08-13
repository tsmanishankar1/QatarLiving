using Dapr.Actors.Runtime;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s.Subscription;
using QLN.Common.Infrastructure.IService.IProductService;
using QLN.Common.Infrastructure.Model; // SubscriptionQuota / ActionTypes
using System.Collections.Concurrent;

namespace QLN.Subscriptions.Actor.ActorClass
{
    public class V2UserAddonActor : Dapr.Actors.Runtime.Actor, IV2UserAddonActor
    {
        private const string V2StateKey = "v2-addon-data";
        private readonly ILogger<V2UserAddonActor> _logger;

        private static readonly ConcurrentDictionary<string, V2UserAddonDto> _mem = new();
        private static volatile bool _storeUnstable = false;
        private static DateTime _lastStoreFailure = DateTime.MinValue;
        private static readonly TimeSpan _break = TimeSpan.FromMinutes(1);
        private static readonly SemaphoreSlim _throttle = new(5, 5);

        public V2UserAddonActor(ActorHost host, ILogger<V2UserAddonActor> logger) : base(host)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> FastSetDataAsync(V2UserAddonDto data, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(data);
            var key = Id.ToString();

            data.lastUpdated = DateTime.UtcNow;
            _mem[key] = data;

            if (_storeUnstable && DateTime.UtcNow - _lastStoreFailure < _break)
                return true;

            if (await _throttle.WaitAsync(TimeSpan.FromMilliseconds(500), ct))
            {
                try
                {
                    using var t = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                    using var linked = CancellationTokenSource.CreateLinkedTokenSource(t.Token, ct);
                    await StateManager.SetStateAsync(V2StateKey, data, linked.Token);
                    await StateManager.SaveStateAsync(linked.Token);
                    _storeUnstable = false;
                }
                catch (Exception)
                {
                    _storeUnstable = true;
                    _lastStoreFailure = DateTime.UtcNow;
                }
                finally
                {
                    _throttle.Release();
                }
            }
            return true;
        }

        public Task<bool> SetDataAsync(V2UserAddonDto data, CancellationToken ct = default)
            => FastSetDataAsync(data, ct);

        public async Task<V2UserAddonDto?> GetDataAsync(CancellationToken ct = default)
        {
            var key = Id.ToString();
            if (_mem.TryGetValue(key, out var cached))
                return cached;

            if (_storeUnstable && DateTime.UtcNow - _lastStoreFailure < _break)
                return null;

            if (await _throttle.WaitAsync(TimeSpan.FromMilliseconds(500), ct))
            {
                try
                {
                    using var t = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                    using var linked = CancellationTokenSource.CreateLinkedTokenSource(t.Token, ct);
                    var val = await StateManager.TryGetStateAsync<V2UserAddonDto>(V2StateKey, linked.Token);
                    if (val.HasValue && val.Value != null)
                    {
                        _mem[key] = val.Value;
                        _storeUnstable = false;
                        return val.Value;
                    }
                }
                catch (Exception)
                {
                    _storeUnstable = true;
                    _lastStoreFailure = DateTime.UtcNow;
                }
                finally
                {
                    _throttle.Release();
                }
            }
            return null;
        }

        public async Task<bool> ValidateUsageAsync(string quotaType, int requestedAmount, CancellationToken ct = default)
        {
            var data = await GetDataAsync(ct);
            if (data == null) return false;

            if (data.StatusId != V2Status.Active || data.EndDate <= DateTime.UtcNow)
                return false;

            var qty = requestedAmount;
            var action = MapQuotaTypeToAction(quotaType);
            var res = data.Quota.ValidateAction(action, qty);
            return res.IsValid;
        }

        public async Task<bool> RecordUsageAsync(string quotaType, int amount, CancellationToken ct = default)
        {
            var data = await GetDataAsync(ct);
            if (data == null) return false;

            if (data.StatusId != V2Status.Active || data.EndDate <= DateTime.UtcNow)
                return false;

            var qty = amount;
            var action = MapQuotaTypeToAction(quotaType);
            var ok = data.Quota.RecordUsage(action, qty);
            if (!ok) return false;

            return await FastSetDataAsync(data, ct);
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
