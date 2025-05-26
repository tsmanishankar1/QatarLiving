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


    public async Task<List<SubscriptionResponseDto>> GetSubscriptionsByVerticalAndCategoryAsync(int verticalTypeId, int categoryId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting subscriptions with VerticalTypeId: {VerticalTypeId} and CategoryId: {CategoryId}", verticalTypeId, categoryId);

        var allIds = _subscriptionIds.Keys.ToList();
        var matchingSubscriptions = new List<SubscriptionResponseDto>();

        if (allIds.Count == 0)
        {
            _logger.LogWarning("No subscriptions available to search");
            return matchingSubscriptions;
        }

        foreach (var id in allIds)
        {
            var actor = GetActorProxy(id);
            var dto = await actor.GetDataAsync(cancellationToken);

            if (dto == null)
                continue;

            if (dto.verticalTypeId == verticalTypeId && dto.categoryId == categoryId && dto.statusId != 3)
            {
                matchingSubscriptions.Add(new SubscriptionResponseDto
                {
                    Id = dto.Id,
                    SubscriptionName = dto.subscriptionName,
                    Duration = dto.duration,
                    Price = dto.price,
                    Description = dto.description,
                    Currency = dto.currency
                });
            }
        }

        if (matchingSubscriptions.Count == 0)
        {
            _logger.LogWarning("No subscriptions found for VerticalTypeId {VerticalTypeId} and CategoryId {CategoryId}", verticalTypeId, categoryId);
        }

        return matchingSubscriptions;
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
    public async Task<Guid> CreatePaymentAsync(PaymentTransactionRequestDto request, Guid userId, CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        var id = Guid.NewGuid();
        var startDate = DateTime.UtcNow;
        var subscriptionactor = GetActorProxy(request.SubscriptionId);
        var subscriptionData = await subscriptionactor.GetDataAsync(cancellationToken);
         if (subscriptionData == null)
                throw new Exception($"PayToPublish data not found for ID: {request.SubscriptionId}");
            var durationText = subscriptionData.duration;
            var endDate = ParseDurationAndGetEndDate(startDate, durationText);
        var dto = new PaymentTransactionDto
        {
            Id = id,
            SubscriptionId = request.SubscriptionId,
            VerticalId = request.VerticalId,
            CategoryId = request.CategoryId,
            CardNumber = request.CardNumber,
            ExpiryMonth = request.ExpiryMonth,
            ExpiryYear = request.ExpiryYear,
            UserId = userId,
            StartDate = startDate,
            EndDate = endDate,
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
    private DateTime ParseDurationAndGetEndDate(DateTime startDate, string duration)
    {
        if (string.IsNullOrWhiteSpace(duration))
            throw new ArgumentException("Duration is empty or null", nameof(duration));

        duration = duration.ToLowerInvariant();
        var digits = new string(duration.Where(char.IsDigit).ToArray());

        if (string.IsNullOrWhiteSpace(digits))
            throw new ArgumentException($"No digits found in duration: {duration}");

        int value = int.Parse(digits);

        if (duration.Contains("month"))
        {
            return startDate.AddMonths(value);
        }

        if (duration.Contains("year"))
        {
            return startDate.AddYears(value);
        }

        throw new ArgumentException($"Unsupported duration format: {duration}");
    }
    private IPaymentTransactionActor GetPaymentTransactionActorProxy(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Actor ID cannot be empty", nameof(id));

        return ActorProxy.Create<IPaymentTransactionActor>(
            new ActorId(id.ToString()),
            ("PaymentTransactionActor"));

       
    }
    public async Task<SubscriptionDetailsResponseDto?> GetSubscriptionDetailsByVerticalIdAsync(
    int verticalId,
    CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching subscription details for verticalId: {VerticalId}", verticalId);

        // Check if verticalId is 3 (or add other valid IDs as needed)
        if (verticalId == 3)
        {
            _logger.LogInformation("Returning mocked subscription details for verticalId: {VerticalId}", verticalId);

            return new SubscriptionDetailsResponseDto
            {
                CompanyId = Guid.NewGuid(),
                CompanyName = "Preloved",
                BusinessProfile = new BusinessProfileDto
                {
                    Name = "Luxury Store",
                    CompanyId = Guid.NewGuid(),
                    CompanyName = "Preloved",
                    Duration = "6 months",
                    ValidFrom = DateTime.UtcNow.Date,
                    ValidTo = DateTime.UtcNow.Date.AddMonths(6),
                    LogoUrl = "images/subscription/CompanyLogo.svg"
                },
                SubscriptionStatistics = new SubscriptionStatisticsDto
                {
                    PublishedAds = new UsageDto { Usage = 5, Total = 15 },
                    PromotedAds = new UsageDto { Usage = 1, Total = 2 },
                    FeaturedAds = new UsageDto { Usage = 2, Total = 2 },
                    Refreshes = new UsageDto { Usage = 5, Total = 75 }
                }
            };
        }

        _logger.LogWarning("No subscription found for verticalId: {VerticalId}", verticalId);
        return null;
    }



}

