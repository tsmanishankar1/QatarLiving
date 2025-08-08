using Dapr.Actors.Runtime;
using QLN.Common.DTO_s.Subscription;
using QLN.Common.Infrastructure.IService.IProductService;
using System.Collections.Concurrent;

namespace QLN.Subscriptions.Actor.ActorClass
{
    public class V2UserAddonActor : Dapr.Actors.Runtime.Actor, IV2UserAddonActor
    {
        private const string V2AddonStateKey = "v2-addon-data";
        private readonly ILogger<V2UserAddonActor> _logger;

        // Memory cache pattern for performance
        private static readonly ConcurrentDictionary<string, V2UserAddonDto> _v2AddonMemoryCache = new();
        private static volatile bool _v2AddonStateStoreUnstable = false;
        private static DateTime _v2AddonLastStateStoreFailure = DateTime.MinValue;
        private static readonly TimeSpan _v2AddonCircuitBreakDuration = TimeSpan.FromMinutes(1);
        private static readonly SemaphoreSlim _v2AddonStateStoreThrottle = new(5, 5);

        public V2UserAddonActor(ActorHost host, ILogger<V2UserAddonActor> logger) : base(host)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> FastSetDataAsync(V2UserAddonDto data, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(data);
            var start = DateTime.UtcNow;
            var actorKey = Id.ToString();

            try
            {
                _logger.LogInformation("[V2AddonActor {ActorId}] FastSetDataAsync started for addon {ProductCode}",
                    actorKey, data.ProductCode);

                data.lastUpdated = DateTime.UtcNow;
                _v2AddonMemoryCache[actorKey] = data;

                // Check circuit breaker
                if (_v2AddonStateStoreUnstable && DateTime.UtcNow - _v2AddonLastStateStoreFailure < _v2AddonCircuitBreakDuration)
                {
                    _logger.LogWarning("[V2AddonActor {ActorId}] State store circuit breaker active - using memory cache only", actorKey);
                    return true;
                }

                // Try to save to state store with throttling
                if (await _v2AddonStateStoreThrottle.WaitAsync(TimeSpan.FromMilliseconds(500), cancellationToken))
                {
                    try
                    {
                        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);

                        await StateManager.SetStateAsync(V2AddonStateKey, data, linkedCts.Token);
                        await StateManager.SaveStateAsync(linkedCts.Token);

                        _v2AddonStateStoreUnstable = false;
                        _logger.LogInformation("[V2AddonActor {ActorId}] Data saved to state store successfully", actorKey);
                    }
                    catch (Exception ex) when (ex is OperationCanceledException || ex is TaskCanceledException)
                    {
                        _v2AddonStateStoreUnstable = true;
                        _v2AddonLastStateStoreFailure = DateTime.UtcNow;
                        _logger.LogWarning(ex, "[V2AddonActor {ActorId}] State store operation failed, using memory cache", actorKey);
                    }
                    finally
                    {
                        _v2AddonStateStoreThrottle.Release();
                    }
                }

                var duration = DateTime.UtcNow - start;
                _logger.LogInformation("[V2AddonActor {ActorId}] FastSetDataAsync completed in {Duration}ms",
                    actorKey, duration.TotalMilliseconds);

                return true;
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - start;
                _logger.LogError(ex, "[V2AddonActor {ActorId}] Critical error in FastSetDataAsync after {Duration}ms",
                    actorKey, duration.TotalMilliseconds);
                throw;
            }
        }

        public async Task<bool> SetDataAsync(V2UserAddonDto data, CancellationToken cancellationToken = default)
        {
            return await FastSetDataAsync(data, cancellationToken);
        }

        public async Task<V2UserAddonDto?> GetDataAsync(CancellationToken cancellationToken = default)
        {
            var actorKey = Id.ToString();

            try
            {
                _logger.LogInformation("[V2AddonActor {ActorId}] GetDataAsync started", actorKey);

                // Check memory cache first
                if (_v2AddonMemoryCache.TryGetValue(actorKey, out var cachedData))
                {
                    _logger.LogInformation("[V2AddonActor {ActorId}] Returning data from memory cache", actorKey);
                    return cachedData;
                }

                // Check circuit breaker
                if (_v2AddonStateStoreUnstable && DateTime.UtcNow - _v2AddonLastStateStoreFailure < _v2AddonCircuitBreakDuration)
                {
                    _logger.LogWarning("[V2AddonActor {ActorId}] State store circuit breaker active and no data in memory cache", actorKey);
                    return null;
                }

                // Try to get from state store
                if (await _v2AddonStateStoreThrottle.WaitAsync(TimeSpan.FromMilliseconds(500), cancellationToken))
                {
                    try
                    {
                        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);

                        var conditionalValue = await StateManager.TryGetStateAsync<V2UserAddonDto>(V2AddonStateKey, linkedCts.Token);

                        if (!conditionalValue.HasValue)
                        {
                            _logger.LogInformation("[V2AddonActor {ActorId}] No data found in state store", actorKey);
                            return null;
                        }

                        var data = conditionalValue.Value;
                        if (data != null)
                        {
                            _v2AddonMemoryCache[actorKey] = data;
                            _logger.LogInformation("[V2AddonActor {ActorId}] Data loaded from state store and cached", actorKey);
                        }

                        _v2AddonStateStoreUnstable = false;
                        return data;
                    }
                    catch (Exception ex) when (ex is OperationCanceledException || ex is TaskCanceledException)
                    {
                        _v2AddonStateStoreUnstable = true;
                        _v2AddonLastStateStoreFailure = DateTime.UtcNow;
                        _logger.LogWarning(ex, "[V2AddonActor {ActorId}] State store operation failed", actorKey);
                        return null;
                    }
                    finally
                    {
                        _v2AddonStateStoreThrottle.Release();
                    }
                }
                else
                {
                    _logger.LogWarning("[V2AddonActor {ActorId}] State store semaphore timeout", actorKey);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V2AddonActor {ActorId}] Critical error in GetDataAsync", actorKey);
                throw;
            }
        }

        public async Task<bool> ValidateUsageAsync(string quotaType, decimal requestedAmount, CancellationToken cancellationToken = default)
        {
            try
            {
                var data = await GetDataAsync(cancellationToken);
                if (data == null)
                {
                    _logger.LogWarning("[V2AddonActor {Id}] Addon not found for usage validation", Id);
                    return false;
                }

                // Check if addon is active and not expired
                if (data.StatusId != V2Status.Active || data.EndDate <= DateTime.UtcNow)
                {
                    _logger.LogWarning("[V2AddonActor {Id}] Addon is not active or expired. Status: {Status}, EndDate: {EndDate}",
                        Id, data.StatusId, data.EndDate);
                    return false;
                }

                // Get current quota for the specified type
                var currentQuota = GetCurrentQuota(data, quotaType);
                var hasEnoughQuota = currentQuota >= requestedAmount;

                _logger.LogInformation("[V2AddonActor {Id}] Usage validation: {QuotaType}={CurrentQuota}, Requested={RequestedAmount}, Valid={IsValid}",
                    Id, quotaType, currentQuota, requestedAmount, hasEnoughQuota);

                return hasEnoughQuota;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V2AddonActor {Id}] Error validating usage", Id);
                return false;
            }
        }

        public async Task<bool> RecordUsageAsync(string quotaType, decimal amount, CancellationToken cancellationToken = default)
        {
            try
            {
                var data = await GetDataAsync(cancellationToken);
                if (data == null)
                {
                    _logger.LogWarning("[V2AddonActor {Id}] Addon not found for usage recording", Id);
                    return false;
                }

                // Check if addon is active
                if (data.StatusId != V2Status.Active || data.EndDate <= DateTime.UtcNow)
                {
                    _logger.LogWarning("[V2AddonActor {Id}] Cannot record usage for inactive/expired addon", Id);
                    return false;
                }

                // Decrement quota
                var success = DecrementQuota(data, quotaType, amount);
                if (!success)
                {
                    _logger.LogWarning("[V2AddonActor {Id}] Failed to decrement quota {QuotaType} by {Amount}", Id, quotaType, amount);
                    return false;
                }

                // Save updated data
                var result = await FastSetDataAsync(data, cancellationToken);

                _logger.LogInformation("[V2AddonActor {Id}] Usage recorded: {Amount} {QuotaType}. Remaining: {Remaining}",
                    Id, amount, quotaType, GetCurrentQuota(data, quotaType));

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V2AddonActor {Id}] Error recording usage", Id);
                return false;
            }
        }

        public async Task<bool> IsActiveAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var data = await GetDataAsync(cancellationToken);
                var isActive = data != null &&
                              data.StatusId == V2Status.Active &&
                              data.EndDate > DateTime.UtcNow;

                _logger.LogInformation("[V2AddonActor {Id}] IsActive check: {IsActive}", Id, isActive);
                return isActive;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V2AddonActor {Id}] Error checking if addon is active", Id);
                return false;
            }
        }

        #region Helper Methods

        private decimal GetCurrentQuota(V2UserAddonDto data, string quotaType)
        {
            if (data.Quota.TryGetValue(quotaType, out var quotaValue))
            {
                if (decimal.TryParse(quotaValue, out var result))
                {
                    return result;
                }
            }

            _logger.LogWarning("[V2AddonActor {Id}] Quota type '{QuotaType}' not found or invalid", Id, quotaType);
            return 0;
        }

        private bool DecrementQuota(V2UserAddonDto data, string quotaType, decimal amount)
        {
            if (!data.Quota.TryGetValue(quotaType, out var quotaValue))
            {
                _logger.LogWarning("[V2AddonActor {Id}] Quota type '{QuotaType}' not found", Id, quotaType);
                return false;
            }

            if (!decimal.TryParse(quotaValue, out var currentQuota))
            {
                _logger.LogWarning("[V2AddonActor {Id}] Invalid quota value for '{QuotaType}': {Value}", Id, quotaType, quotaValue);
                return false;
            }

            if (currentQuota < amount)
            {
                _logger.LogWarning("[V2AddonActor {Id}] Insufficient quota. Current: {Current}, Requested: {Requested}",
                    Id, currentQuota, amount);
                return false;
            }

            var newQuota = Math.Max(0, currentQuota - amount);
            data.Quota[quotaType] = newQuota.ToString();
            data.lastUpdated = DateTime.UtcNow;

            return true;
        }

        #endregion

        #region Actor Lifecycle

        protected override Task OnActivateAsync()
        {
            _logger.LogInformation("[V2AddonActor {Id}] Actor activated", Id);
            return base.OnActivateAsync();
        }

        protected override Task OnDeactivateAsync()
        {
            _logger.LogInformation("[V2AddonActor {Id}] Actor deactivated", Id);
            return base.OnDeactivateAsync();
        }

        #endregion
    }
}