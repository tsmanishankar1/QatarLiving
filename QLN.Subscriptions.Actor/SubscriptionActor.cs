using Dapr.Actors.Client;
using Dapr.Actors;
using Dapr.Actors.Runtime;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Subscriptions;

using System.Collections.Concurrent;
using System.Net.Sockets;


namespace QLN.Subscriptions
{
    public class SubscriptionActor : Actor, ISubscriptionActor
    {
        private const string StateKey = "subscription-data";
        private readonly ILogger<SubscriptionActor> _logger;

        // Further reduced timeout to avoid waiting too long for state store operations
        private readonly TimeSpan _stateOperationTimeout = TimeSpan.FromSeconds(2);

        // In-memory cache to avoid state store operations when possible
        private static readonly ConcurrentDictionary<string, SubscriptionDto> _memoryCache = new ConcurrentDictionary<string, SubscriptionDto>();

        // Flag to track if we're experiencing state store issues
        private static volatile bool _stateStoreUnstable = false;
        private static DateTime _lastStateStoreFailure = DateTime.MinValue;
        private static readonly TimeSpan _circuitBreakDuration = TimeSpan.FromMinutes(1); // Reduced from 2 minutes to 1 minute

        // Keep track of pending operations to avoid flooding the state store
        private static readonly SemaphoreSlim _stateStoreThrottle = new SemaphoreSlim(5, 5); // Max 5 concurrent state operations

        public SubscriptionActor(ActorHost host, ILogger<SubscriptionActor> logger) : base(host)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        private ISubscriptionActor GetActorProxy(Guid subscriptionId)
        {
            var actorId = new ActorId(subscriptionId.ToString());

            return ActorProxy.Create<ISubscriptionActor>(actorId, nameof(SubscriptionActor));
        }

        public async Task<bool> FastSetDataAsync(SubscriptionDto data, CancellationToken cancellationToken = default)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            var start = DateTime.UtcNow;
            var actorKey = Id.ToString();

            try
            {
                _logger.LogInformation("[Actor {ActorId}] FastSetDataAsync started for subscription {Name}", actorKey, data.Name);

                // Update timestamp
                data.LastUpdated = DateTime.UtcNow;

                // ALWAYS update our in-memory cache first to ensure requests succeed
                _memoryCache[actorKey] = data;

                // Skip state store operations if circuit breaker is active
                if (_stateStoreUnstable && DateTime.UtcNow - _lastStateStoreFailure < _circuitBreakDuration)
                {
                    _logger.LogWarning("[Actor {ActorId}] State store circuit breaker active - using memory cache only", actorKey);
                    return true;
                }

                // Only try to access the state store if we can acquire the semaphore without waiting too long
                if (await _stateStoreThrottle.WaitAsync(TimeSpan.FromMilliseconds(500), cancellationToken))
                {
                    try
                    {
                        // Use very short timeout for state operation
                        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(1)); // Reduced from 2 seconds
                        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);

                        await StateManager.SetStateAsync(StateKey, data, linkedCts.Token);

                        // Reset state store stability flag if operation succeeds
                        _stateStoreUnstable = false;
                    }
                    catch (Exception ex) when (
                        ex is OperationCanceledException ||
                        ex is SocketException ||
                        ex.InnerException is SocketException)
                    {
                        // Mark state store as unstable but continue since we have the data in memory
                        _stateStoreUnstable = true;
                        _lastStateStoreFailure = DateTime.UtcNow;
                        _logger.LogWarning(ex, "[Actor {ActorId}] State store operation failed, activating circuit breaker, data preserved in memory", actorKey);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[Actor {ActorId}] State store operation error in FastSetDataAsync, using memory cache only", actorKey);
                    }
                    finally
                    {
                        _stateStoreThrottle.Release();
                    }
                }
                else
                {
                    _logger.LogWarning("[Actor {ActorId}] State store semaphore timeout, using memory cache only", actorKey);
                }

                var duration = DateTime.UtcNow - start;
                _logger.LogInformation("[Actor {ActorId}] FastSetDataAsync completed in {Duration}ms for subscription {Id}",
                    actorKey, duration.TotalMilliseconds, data.Id);

                return true;
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - start;
                _logger.LogError(ex, "[Actor {ActorId}] Critical error in FastSetDataAsync after {Duration}ms for subscription {Id}",
                    Id, duration.TotalMilliseconds, data.Id);
                throw;
            }
        }

        public async Task<bool> SetDataAsync(SubscriptionDto data, CancellationToken cancellationToken = default)
        {
            // IMPORTANT FIX: For immediate user-facing operations, use FastSetDataAsync to avoid delays
            // This removes a major source of request delays
            return await FastSetDataAsync(data, cancellationToken);
        }

        public async Task<SubscriptionDto?> GetDataAsync(CancellationToken cancellationToken = default)
        {
            var start = DateTime.UtcNow;
            var actorKey = Id.ToString();

            try
            {
                _logger.LogInformation("[Actor {ActorId}] GetDataAsync started", actorKey);

                // First check memory cache for data - this is the performance critical path
                if (_memoryCache.TryGetValue(actorKey, out var cachedData))
                {
                    var cacheDuration = DateTime.UtcNow - start;
                    _logger.LogInformation("[Actor {ActorId}] GetDataAsync returning data from memory cache in {Duration}ms",
                        actorKey, cacheDuration.TotalMilliseconds);
                    return cachedData;
                }

                // Check if state store is experiencing issues
                if (_stateStoreUnstable && DateTime.UtcNow - _lastStateStoreFailure < _circuitBreakDuration)
                {
                    _logger.LogWarning("[Actor {ActorId}] GetDataAsync - State store circuit breaker active and no data in memory cache", actorKey);
                    return null;
                }

                // Only try to access the state store if we can acquire the semaphore without waiting too long
                if (await _stateStoreThrottle.WaitAsync(TimeSpan.FromMilliseconds(500), cancellationToken))
                {
                    try
                    {
                        // Use very short timeout for state operation
                        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(1)); // Reduced from 2 seconds
                        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);

                        if (!await StateManager.ContainsStateAsync(StateKey, linkedCts.Token))
                        {
                            _logger.LogInformation("[Actor {ActorId}] GetDataAsync found no data in state store", actorKey);
                            return null;
                        }

                        var data = await StateManager.GetStateAsync<SubscriptionDto>(StateKey, linkedCts.Token);

                        // Store in memory cache for future requests
                        if (data != null)
                        {
                            _memoryCache[actorKey] = data;
                        }

                        // Reset state store stability flag since operation succeeded
                        _stateStoreUnstable = false;

                        var duration = DateTime.UtcNow - start;
                        _logger.LogInformation("[Actor {ActorId}] GetDataAsync completed from state store in {Duration}ms for subscription {Id}",
                            actorKey, duration.TotalMilliseconds, data?.Id);

                        return data;
                    }
                    catch (Exception ex) when (
                        ex is OperationCanceledException ||
                        ex is SocketException ||
                        ex.InnerException is SocketException)
                    {
                        // Mark state store as unstable
                        _stateStoreUnstable = true;
                        _lastStateStoreFailure = DateTime.UtcNow;
                        _logger.LogWarning(ex, "[Actor {ActorId}] State store operation failed in GetDataAsync, activating circuit breaker", actorKey);
                        return null;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[Actor {ActorId}] Error accessing state store in GetDataAsync", actorKey);
                        return null;
                    }
                    finally
                    {
                        _stateStoreThrottle.Release();
                    }
                }
                else
                {
                    _logger.LogWarning("[Actor {ActorId}] State store semaphore timeout for GetDataAsync", actorKey);
                    return null;
                }
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - start;
                _logger.LogError(ex, "[Actor {ActorId}] Critical error in GetDataAsync after {Duration}ms",
                    actorKey, duration.TotalMilliseconds);
                throw;
            }
        }

        public async Task<bool> ExpireSubscription(CancellationToken cancellationToken = default)
        {
            var start = DateTime.UtcNow;
            var actorKey = Id.ToString();

            try
            {
                _logger.LogInformation("[Actor {ActorId}] ExpireSubscription started", actorKey);

                // First check memory cache
                SubscriptionDto? subscription = null;
                if (_memoryCache.TryGetValue(actorKey, out var cachedData))
                {
                    subscription = cachedData;
                    _logger.LogInformation("[Actor {ActorId}] ExpireSubscription using data from memory cache", actorKey);
                }
                else if (!_stateStoreUnstable || DateTime.UtcNow - _lastStateStoreFailure >= _circuitBreakDuration)
                {
                    // Only try to get from state store if it's considered stable
                    // And only if we can acquire the semaphore without waiting too long
                    if (await _stateStoreThrottle.WaitAsync(TimeSpan.FromMilliseconds(500), cancellationToken))
                    {
                        try
                        {
                            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);

                            if (await StateManager.ContainsStateAsync(StateKey, linkedCts.Token))
                            {
                                subscription = await StateManager.GetStateAsync<SubscriptionDto>(StateKey, linkedCts.Token);
                                _logger.LogInformation("[Actor {ActorId}] ExpireSubscription loaded data from state store", actorKey);
                            }
                        }
                        catch (Exception ex) when (
                            ex is OperationCanceledException ||
                            ex is SocketException ||
                            ex.InnerException is SocketException)
                        {
                            _stateStoreUnstable = true;
                            _lastStateStoreFailure = DateTime.UtcNow;
                            _logger.LogWarning(ex, "[Actor {ActorId}] State store operation failed while getting data for ExpireSubscription", actorKey);
                        }
                        finally
                        {
                            _stateStoreThrottle.Release();
                        }
                    }
                }

                if (subscription == null)
                {
                    // If no subscription found, create a minimal one to be more resilient
                    subscription = new SubscriptionDto
                    {
                        Id = Guid.Parse(actorKey),
                        Name = $"ExpiredSubscription-{actorKey}",
                        Status = SubscriptionStatus.Active,
                        StartDate = DateTime.UtcNow.AddDays(-30) // Assume it started a while ago
                    };
                    _logger.LogWarning("[Actor {ActorId}] No subscription found, creating minimal one for expiration", actorKey);
                }

                // Update the subscription
                subscription.Status = SubscriptionStatus.Expired;
                subscription.EndDate = DateTime.UtcNow;
                subscription.LastUpdated = DateTime.UtcNow;

                // Update via FastSetDataAsync to avoid blocking
                return await FastSetDataAsync(subscription, cancellationToken);
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - start;
                _logger.LogError(ex, "[Actor {ActorId}] Critical error in ExpireSubscription after {Duration}ms",
                    actorKey, duration.TotalMilliseconds);
                throw;
            }
        }

        protected override Task OnActivateAsync()
        {
            _logger.LogInformation("[Actor {ActorId}] Actor activated", Id);
            return base.OnActivateAsync();
        }

        protected override Task OnDeactivateAsync()
        {
            _logger.LogInformation("[Actor {ActorId}] Actor deactivated", Id);
            return base.OnDeactivateAsync();
        }
    }
}
