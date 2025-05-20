using Dapr.Actors;
using Dapr.Actors.Client;

using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Subscriptions;

public class SubscriptionService : ISubscriptionService
{
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

    public SubscriptionService(ILogger<SubscriptionService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private ISubscriptionActor GetActorProxy(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Actor ID cannot be empty", nameof(id));

        return ActorProxy.Create<ISubscriptionActor>(
            new ActorId(id.ToString()),
            "SubscriptionActor");
    }

    public async Task<SubscriptionDto?> GetSubscriptionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting subscription with ID: {Id}", id);
        var actor = GetActorProxy(id);
        return await actor.GetDataAsync(cancellationToken);
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
            return await actor.FastSetDataAsync(subscription, cancellationToken);
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

        return await actor.FastSetDataAsync(subscription, cancellationToken);
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
        return await actor.FastSetDataAsync(minimalSubscription, cancellationToken);
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

            return await actor.FastSetDataAsync(minimalSubscription, cancellationToken);
        }
    }
}