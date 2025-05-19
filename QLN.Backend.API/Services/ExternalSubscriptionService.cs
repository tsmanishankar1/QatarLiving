using Dapr.Actors;
using Dapr.Actors.Client;

using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Subscriptions;

public class SubscriptionService : ISubscriptionService
{
    private readonly IActorProxyFactory _actorProxyFactory;
    private readonly ILogger<SubscriptionService> _logger;
    private readonly int _maxRetries = 3;
    private readonly TimeSpan _initialRetryDelay = TimeSpan.FromMilliseconds(50); 
    private readonly TimeSpan _operationTimeout = TimeSpan.FromSeconds(3);
    private int _consecutiveFailures = 0;
    private readonly int _healthMonitoringThreshold = 5;
    private DateTime _lastSuccess = DateTime.UtcNow;
    private bool _circuitOpen = false;
    private DateTime _circuitResetTime = DateTime.MinValue;
    private readonly TimeSpan _circuitResetDuration = TimeSpan.FromSeconds(30); 
    private readonly int _circuitBreakThreshold = 5; 

    public SubscriptionService(IActorProxyFactory actorProxyFactory, ILogger<SubscriptionService> logger)
    {
        _actorProxyFactory = actorProxyFactory ?? throw new ArgumentNullException(nameof(actorProxyFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private ISubscriptionActor GetActorProxy(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Actor ID cannot be empty", nameof(id));

        return _actorProxyFactory.CreateActorProxy<ISubscriptionActor>(
            new ActorId(id.ToString()),
            nameof(ISubscriptionActor));
    }

    // Circuit breaker pattern implementation
    private bool IsCircuitOpen()
    {
        if (_circuitOpen && DateTime.UtcNow > _circuitResetTime)
        {
            _circuitOpen = false; 
            _logger.LogInformation("Circuit breaker reset. Switching to normal operation mode.");
            return false;
        }
        return _circuitOpen;
    }

    private void CheckCircuitBreaker()
    {
        if (_consecutiveFailures >= _circuitBreakThreshold && !_circuitOpen)
        {
            _circuitOpen = true;
            _circuitResetTime = DateTime.UtcNow.Add(_circuitResetDuration);
            _logger.LogWarning("Circuit breaker triggered. Using fast operations exclusively for {Duration}.", _circuitResetDuration);
        }
    }

    private async Task<T> ExecuteWithRetryAsync<T>(
        Func<CancellationToken, Task<T>> func,
        Func<CancellationToken, Task<T>>? fallbackFunc,
        string operation,
        Guid id,
        CancellationToken cancellationToken)
    {
        var start = DateTime.UtcNow;
        Exception? lastException = null;

 
        if (fallbackFunc != null)
        {
            _logger.LogInformation("Using fallback (fast) operation by default for {Operation} on subscription {Id}", operation, id);
            try
            {
                // Use shorter timeout for fast operations
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(_operationTimeout / 2);

                var result = await fallbackFunc(timeoutCts.Token);
                var duration = DateTime.UtcNow - start;

                _consecutiveFailures = 0;
                _lastSuccess = DateTime.UtcNow;

                _logger.LogInformation("Fast {Operation} succeeded in {Duration}ms for subscription {Id}",
                    operation, duration.TotalMilliseconds, id);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fast operation {Operation} failed for subscription {Id}, falling back to retry pattern", operation, id);
                // Continue with retry pattern below
            }
        }

        // Apply very aggressive retry with minimal delay
        for (int attempt = 1; attempt <= _maxRetries; attempt++)
        {
            try
            {
                _logger.LogInformation("Attempt {Attempt} for {Operation} on subscription {Id}", attempt, operation, id);

                // Create a timeout token source that's linked to the passed cancellation token
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                // Always use short timeouts to avoid request buildup
                var timeout = _operationTimeout;
                if (attempt > 1)
                {
                    // Even shorter timeout for retries
                    timeout = TimeSpan.FromSeconds(1.5);
                }
                timeoutCts.CancelAfter(timeout);

                var operationStart = DateTime.UtcNow;

                // Always try fallback after first attempt if available
                var useFallback = attempt > 1 && fallbackFunc != null;
                var result = await (useFallback ? fallbackFunc : func)(timeoutCts.Token);
                var duration = DateTime.UtcNow - operationStart;

                // Track successful operation
                _consecutiveFailures = 0;
                _lastSuccess = DateTime.UtcNow;

                if (useFallback)
                {
                    _logger.LogInformation("Fallback {Operation} succeeded in {Duration}ms for subscription {Id} (attempt {Attempt})",
                        operation, duration.TotalMilliseconds, id, attempt);
                }
                else
                {
                    _logger.LogInformation("{Operation} succeeded in {Duration}ms for subscription {Id} (attempt {Attempt})",
                        operation, duration.TotalMilliseconds, id, attempt);
                }

                return result;
            }
            catch (OperationCanceledException ex)
            {
                lastException = ex;
                _consecutiveFailures++;
                CheckCircuitBreaker();

                var duration = DateTime.UtcNow - start;
                _logger.LogWarning(ex, "{Operation} timed out after {Duration}ms for subscription {Id} (attempt {Attempt}/{MaxRetries})",
                    operation, duration.TotalMilliseconds, id, attempt, _maxRetries);

                // CRITICAL FIX: Try fallback immediately on timeout if available
                if (fallbackFunc != null)
                {
                    _logger.LogWarning("Trying fallback operation after timeout for {Operation} - subscription {Id}", operation, id);
                    try
                    {
                        using var fallbackTimeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        fallbackTimeoutCts.CancelAfter(TimeSpan.FromSeconds(1.5)); // Very short timeout for fallback

                        var result = await fallbackFunc(fallbackTimeoutCts.Token);

                        var fallbackDuration = DateTime.UtcNow - start;
                        _logger.LogInformation("Fallback operation succeeded in {Duration}ms for {Operation} - subscription {Id}",
                            fallbackDuration.TotalMilliseconds, operation, id);

                        _consecutiveFailures = 0;
                        return result;
                    }
                    catch (Exception fallbackEx)
                    {
                        _logger.LogError(fallbackEx, "Fallback operation also failed for {Operation} - subscription {Id}", operation, id);
                    }
                }

                if (attempt < _maxRetries)
                {
                    // Apply minimal retry delay - we want to try again quickly
                    var retryDelay = CalculateRetryDelay(attempt);
                    await Task.Delay(retryDelay, cancellationToken);
                }
                else
                {
                    _logger.LogError(ex, "{Operation} timed out on final attempt for subscription {Id} after {TotalDuration}ms",
                        operation, id, (DateTime.UtcNow - start).TotalMilliseconds);

                    throw new TimeoutException(
                        $"{operation} timed out after {_maxRetries} attempts for subscription {id}. Total duration: {(DateTime.UtcNow - start).TotalMilliseconds}ms",
                        ex);
                }
            }
            catch (Exception ex)
            {
                lastException = ex;
                _consecutiveFailures++;
                CheckCircuitBreaker();

                if (attempt < _maxRetries)
                {
                    // Try fallback immediately after any error if available
                    if (fallbackFunc != null)
                    {
                        _logger.LogWarning("Trying fallback operation after error for {Operation} - subscription {Id}", operation, id);
                        try
                        {
                            using var fallbackTimeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                            fallbackTimeoutCts.CancelAfter(TimeSpan.FromSeconds(1.5)); // Very short timeout

                            var result = await fallbackFunc(fallbackTimeoutCts.Token);

                            var fallbackDuration = DateTime.UtcNow - start;
                            _logger.LogInformation("Fallback operation succeeded in {Duration}ms for {Operation} - subscription {Id}",
                                fallbackDuration.TotalMilliseconds, operation, id);

                            _consecutiveFailures = 0;
                            return result;
                        }
                        catch (Exception fallbackEx)
                        {
                            _logger.LogError(fallbackEx, "Fallback operation also failed for {Operation} - subscription {Id}", operation, id);
                        }
                    }

                    var retryDelay = CalculateRetryDelay(attempt);
                    await Task.Delay(retryDelay, cancellationToken);
                }
                else
                {
                    _logger.LogError(ex, "{Operation} failed on final attempt for subscription {Id} after {TotalDuration}ms",
                        operation, id, (DateTime.UtcNow - start).TotalMilliseconds);

                    throw;
                }
            }
        }

        // Should not reach here, but just in case
        throw lastException ?? new InvalidOperationException($"{operation} failed after {_maxRetries} attempts for subscription {id}");
    }

    // Calculate retry delay with minimal delay to avoid long waits
    private TimeSpan CalculateRetryDelay(int attempt)
    {
        // Use minimal delays - linear backoff instead of exponential
        var baseDelay = _initialRetryDelay.TotalMilliseconds;
        var linearFactor = 2;
        var deterministicDelay = baseDelay * (1 + (attempt - 1) * linearFactor);

        // Add small jitter (±10ms)
        var jitter = new Random().Next(-10, 11); // ±10 milliseconds
        var totalDelay = Math.Max(0, deterministicDelay + jitter);

        return TimeSpan.FromMilliseconds(totalDelay);
    }


    public async Task<SubscriptionDto?> GetSubscriptionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting subscription with ID: {Id}", id);
        var actor = GetActorProxy(id);
        return await ExecuteWithRetryAsync(
            (token) => actor.GetDataAsync(token),
            null, // No fallback for get operations
            "GetDataAsync",
            id,
            cancellationToken);
    }

  public async Task<bool> CreateSubscriptionAsync(SubscriptionDto subscription, CancellationToken cancellationToken = default)
{
    if (subscription == null) throw new ArgumentNullException(nameof(subscription));

    _logger.LogInformation("CreateSubscriptionAsync started for subscription: {Name}", subscription.Name);

    if (subscription.Id == Guid.Empty)
    {
        subscription.Id = Guid.NewGuid();
        _logger.LogInformation("Generated new subscription ID: {Id}", subscription.Id);
    }

    if (subscription.StartDate == default)
    {
        subscription.StartDate = DateTime.UtcNow;
        _logger.LogInformation("Set subscription start date: {StartDate}", subscription.StartDate);
    }

    var actor = GetActorProxy(subscription.Id);

        try
        {
            return await ExecuteWithRetryAsync<bool>(
                token => actor.FastSetDataAsync(subscription, token),
                null,
                "FastSetDataAsync (Create)",
                subscription.Id,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling actor.FastSetDataAsync");
            throw;
        }


    }


    public async Task<bool> UpdateSubscriptionAsync(SubscriptionDto subscription, CancellationToken cancellationToken = default)
    {
        if (subscription == null)
            throw new ArgumentNullException(nameof(subscription));

        if (subscription.Id == Guid.Empty)
            throw new ArgumentException("Subscription ID cannot be empty", nameof(subscription.Id));

        _logger.LogInformation("Updating subscription with ID: {Id}", subscription.Id);

        var actor = GetActorProxy(subscription.Id);

        // CRITICAL FIX: Always use FastSetDataAsync directly for all write operations
        return await ExecuteWithRetryAsync(
            (token) => actor.FastSetDataAsync(subscription, token),
            null, // No fallback needed since we're already using the fast path
            "FastSetDataAsync (Update)",
            subscription.Id,
            cancellationToken);
    }

    public async Task<bool> DeleteSubscriptionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Subscription ID cannot be empty", nameof(id));

        _logger.LogInformation("Deleting subscription with ID: {Id}", id);

        var actor = GetActorProxy(id);

        // Create a minimal subscription with only essential fields
        var minimalSubscription = new SubscriptionDto
        {
            Id = id,
            Status = SubscriptionStatus.Deleted,
            LastUpdated = DateTime.UtcNow,
            Name = $"Deleted-{id}" // Provide a default name
        };

        // CRITICAL FIX: Always use FastSetDataAsync for consistent performance
        return await ExecuteWithRetryAsync(
            (token) => actor.FastSetDataAsync(minimalSubscription, token),
            null, // No fallback needed
            "FastSetDataAsync (Delete)",
            id,
            cancellationToken);
    }

    public async Task<bool> ExpireSubscriptionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Subscription ID cannot be empty", nameof(id));

        _logger.LogInformation("Expiring subscription with ID: {Id}", id);

        var actor = GetActorProxy(id);

        try
        {
            // Try direct expire first with short timeout
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(2));

            return await actor.ExpireSubscription(timeoutCts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Direct ExpireSubscription failed for {Id}, using fallback approach", id);

            // Fallback: Create minimal expired subscription
            var minimalSubscription = new SubscriptionDto
            {
                Id = id,
                Status = SubscriptionStatus.Expired,
                EndDate = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow,
                Name = $"Expired-{id}" // Provide a default name
            };

            return await ExecuteWithRetryAsync(
                (token) => actor.FastSetDataAsync(minimalSubscription, token),
                null,
                "FastSetDataAsync (Expire Fallback)",
                id,
                cancellationToken);
        }
    }
}