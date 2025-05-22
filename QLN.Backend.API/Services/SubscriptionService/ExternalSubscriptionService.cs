using Dapr.Actors;
using Dapr.Actors.Client;
using QLN.Common.DTOs;
using QLN.Common.Infrastructure.IService.ISubscriptionService;
using QLN.Common.Infrastructure.Subscriptions;
using System.Collections.Concurrent;

public class ExternalSubscriptionService : IExternalSubscriptionService
{
    private readonly ILogger<ExternalSubscriptionService> _logger;
    private static readonly ConcurrentDictionary<Guid, byte> _subscriptionIds = new ConcurrentDictionary<Guid, byte>();
    private static readonly ConcurrentDictionary<Guid, byte> _subscriptionId = new();


    public ExternalSubscriptionService(ILogger<ExternalSubscriptionService> logger)
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


    public async Task<SubscriptionResponseDto?> GetSubscriptionByVerticalAndCategoryAsync(int verticalTypeId, int categoryId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting subscription with VerticalTypeId: {VerticalTypeId} and CategoryId: {CategoryId}", verticalTypeId, categoryId);

        var allIds = _subscriptionIds.Keys.ToList();

        if (allIds.Count == 0)
        {
            _logger.LogWarning("No subscriptions available to search");
            return null;
        }

        foreach (var id in allIds)
        {
            var actor = GetActorProxy(id);
            var dto = await actor.GetDataAsync(cancellationToken);

            if (dto == null)
                continue;

            if (dto.verticalTypeId == verticalTypeId && dto.categoryId == categoryId && dto.statusId != 3)
            {
                return new SubscriptionResponseDto
                {
                    Id = dto.Id,
                    SubscriptionName = dto.subscriptionName,
                    Duration = dto.duration,
                    Price = dto.price,
                    Description = dto.description,
                    Currency = dto.currency
                };
            }
        }

        _logger.LogWarning("No subscription found for VerticalTypeId {VerticalTypeId} and CategoryId {CategoryId}", verticalTypeId, categoryId);
        return null;
    }

    public async Task CreateSubscriptionAsync(SubscriptionRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        var id = Guid.NewGuid();

        var dto = new SubscriptionDto
        {
            Id = id,
            subscriptionName = request.SubscriptionName,
            duration = request.Duration,
            price = request.Price,
            description = request.Description,
            currency = request.Currency,
            adsbudget = request.adsbudget,
            promotebudget = request.promotebudget,
            refreshbudget = request.refreshbudget,
            categoryId = request.CategoryId,
            verticalTypeId = request.VerticalTypeId,
            statusId = request.StatusId,
            lastUpdated = DateTime.UtcNow
        };

        var actor = GetActorProxy(dto.Id);
        var result = await actor.FastSetDataAsync(dto, cancellationToken);

        if (result)
        {
            _subscriptionIds.TryAdd(dto.Id, 0);
            return;
        }

        throw new Exception("Subscription creation failed.");
    }

    public async Task<List<SubscriptionResponseDto>> GetAllSubscriptionsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting all subscriptions");

        var allIds = _subscriptionIds.Keys.ToList();

        if (allIds.Count == 0)
        {
            _logger.LogWarning("No subscription IDs found in tracking dictionary");
            return new List<SubscriptionResponseDto>();
        }

        _logger.LogInformation("Found {Count} subscription IDs to retrieve", allIds.Count);

        var tasks = allIds.Select(id =>
        {
            var actor = GetActorProxy(id);
            return actor.GetDataAsync(cancellationToken);
        });

        var results = await Task.WhenAll(tasks);

        var subscriptions = new List<SubscriptionResponseDto>();

        foreach (var result in results)
        {
            if (result == null)
                continue;

            if (result.statusId == 3)
            {
                _subscriptionIds.TryRemove(result.Id, out _);
                continue;
            }

            subscriptions.Add(new SubscriptionResponseDto
            {
                Id = result.Id,
                SubscriptionName = result.subscriptionName,
                Duration = result.duration,
                Price = result.price,
                Description = result.description,
                Currency = result.currency
            });
        }

        return subscriptions;
    }

    public async Task<bool> UpdateSubscriptionAsync(Guid subscriptionId, SubscriptionRequestDto request, CancellationToken cancellationToken)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var dto = new SubscriptionDto
        {
            Id = subscriptionId,
            subscriptionName = request.SubscriptionName,
            duration = request.Duration,
            price = request.Price,
            description = request.Description,
            currency = request.Currency,
            categoryId = request.CategoryId,
            verticalTypeId = request.VerticalTypeId,
            statusId = request.StatusId,
            lastUpdated = DateTime.UtcNow
        };

        var actor = GetActorProxy(dto.Id);
        var result = await actor.FastSetDataAsync(dto, cancellationToken);

        if (result)
        {
            _subscriptionIds.TryAdd(dto.Id, 0);
            return true;
        }

        return false;
    }

    public async Task<bool> DeleteSubscriptionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Subscription ID cannot be empty", nameof(id));

        _logger.LogInformation("Deleting subscription with ID: {Id}", id);

        try
        {
            var existingSubscription = await GetSubscriptionByIdAsync(id, cancellationToken);
            if (existingSubscription == null)
            {
                _logger.LogWarning("Subscription with ID {Id} not found for deletion", id);
                return false;
            }

            // Mark as deleted with numeric status
            existingSubscription.statusId = 3;
            existingSubscription.lastUpdated = DateTime.UtcNow;
            existingSubscription.subscriptionName = $"Deleted-{existingSubscription.subscriptionName ?? id.ToString()}";

            var actor = GetActorProxy(id);
            var result = await actor.FastSetDataAsync(existingSubscription, cancellationToken);

            _logger.LogInformation("Subscription with ID {Id} marked as deleted.", id);
            return result;
        }
        catch (KeyNotFoundException knfEx)
        {
            _logger.LogWarning(knfEx, "Key not found for subscription ID: {Id}", id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting subscription with ID: {Id}", id);
            throw;
        }
    }


    private async Task<SubscriptionDto?> GetSubscriptionByIdAsync(Guid subscriptionId, CancellationToken cancellationToken)
    {
        var allIds = _subscriptionIds.Keys.ToList();

        foreach (var id in allIds)
        {
            var actor = GetActorProxy(id);
            var dto = await actor.GetDataAsync(cancellationToken);

            if (dto != null && dto.Id == subscriptionId && dto.statusId != 3)
            {
                return dto;
            }
        }

        return null;
    }
    public async Task<Guid> CreatePaymentAsync(PaymentTransactionRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        var id = Guid.NewGuid();

        var dto = new PaymentTransactionDto
        {
            Id = id,
            SubscriptionId = request.SubscriptionId,
            VerticalId = request.VerticalId,
            CategoryId = request.CategoryId,
            CardNumber = request.CardNumber,
            ExpiryMonth = request.ExpiryMonth,
            ExpiryYear = request.ExpiryYear,
            CardHolderName = request.CardHolderName,
            TransactionDate = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow,
        };

        var actor = GetPaymentTransactionActorProxy(dto.Id);
        var result = await actor.FastSetDataAsync(dto, cancellationToken);

        if (result)
        {
            _subscriptionIds.TryAdd(dto.Id, 0);
            _logger.LogInformation("Payment transaction created with ID: {TransactionId}", dto.Id);
            return dto.Id;
        }

        throw new Exception("Payment transaction creation failed.");
    }

    private IPaymentTransactionActor GetPaymentTransactionActorProxy(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Actor ID cannot be empty", nameof(id));

        return ActorProxy.Create<IPaymentTransactionActor>(
            new ActorId(id.ToString()),
            ("PaymentTransactionActor"));

       
    }
}

