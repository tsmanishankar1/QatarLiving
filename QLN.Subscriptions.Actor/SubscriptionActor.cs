using Dapr.Actors.Client;
using Dapr.Actors;
using Dapr.Actors.Runtime;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Subscriptions;

using System.Collections.Concurrent;
using System.Net.Sockets;
using QLN.Common.DTOs;


namespace QLN.Subscriptions
{
    public class SubscriptionActor : Actor, ISubscriptionActor
    {
        private const string StateKey = "subscription-data";
        private readonly ILogger<SubscriptionActor> _logger;



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

        public async Task<bool> FastSetDataAsync(SubscriptionDto data, CancellationToken cancellationToken = default)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            var start = DateTime.UtcNow;
            var actorKey = Id.ToString();

            try
            {
                _logger.LogInformation("[Actor {ActorId}] FastSetDataAsync started for subscription {Name}", actorKey, data.subscriptionName);

                // Update timestamp
                data.lastUpdated = DateTime.UtcNow;

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
