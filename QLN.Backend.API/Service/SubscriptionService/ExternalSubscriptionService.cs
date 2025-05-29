using Dapr.Actors;
using Dapr.Actors.Client;
using Microsoft.AspNetCore.Identity;
using QLN.Common.DTOs;
using QLN.Common.Infrastructure.IService.ISubscriptionService;
using QLN.Common.Infrastructure.Model;
using QLN.Common.Infrastructure.Subscriptions;
using System.Collections.Concurrent;

public class ExternalSubscriptionService : IExternalSubscriptionService
{
    private readonly ILogger<ExternalSubscriptionService> _logger;
    private static readonly ConcurrentDictionary<Guid, byte> _paymentTransactionIds = new();
    private static readonly ConcurrentDictionary<Guid, byte> _subscriptionIds = new ConcurrentDictionary<Guid, byte>();
    private static readonly ConcurrentDictionary<Guid, byte> _subscriptionId = new();
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;


    public ExternalSubscriptionService(ILogger<ExternalSubscriptionService> logger, RoleManager<IdentityRole<Guid>> roleManager,
     UserManager<ApplicationUser> userManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _roleManager = roleManager;
        _userManager = userManager;
    }

    private ISubscriptionActor GetActorProxy(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Actor ID cannot be empty", nameof(id));

        return ActorProxy.Create<ISubscriptionActor>(
            new ActorId(id.ToString()),
            "SubscriptionActor");
    }
    public async Task<SubscriptionGroupResponseDto> GetSubscriptionsByVerticalAndCategoryAsync(
           int verticalTypeId,
           int categoryId,
           CancellationToken cancellationToken = default)
    {
        var resultList = new List<SubscriptionResponseDto>();
        var ids = _subscriptionIds.Keys.ToList();

        var verticalEnum = (Vertical)verticalTypeId;
        var categoryEnum = (SubscriptionCategory)categoryId;

        foreach (var id in ids)
        {
            var actor = GetActorProxy(id);
            var data = await actor.GetDataAsync(cancellationToken);

            if (data != null &&
                data.VerticalTypeId == verticalEnum &&
                data.CategoryId == categoryEnum &&
                data.StatusId != Status.Expired)
            {
                resultList.Add(new SubscriptionResponseDto
                {
                    Id = data.Id,
                    SubscriptionName = data.subscriptionName,
                    Price = data.price,
                    Currency = data.currency,
                    Description = data.description,
                    Duration = data.duration
                });
            }
        }

        return new SubscriptionGroupResponseDto
        {
            VerticalTypeId = verticalTypeId,
            VerticalName = verticalEnum.ToString(),
            CategoryId = categoryId,
            CategoryName = categoryEnum.ToString(),
            Subscriptions = resultList
        };
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
            CategoryId = request.CategoryId,
            VerticalTypeId = request.VerticalTypeId,
            StatusId =request.StatusId,

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

            if (result.StatusId == Status.Expired)
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
            CategoryId = request.CategoryId,
            VerticalTypeId = request.VerticalTypeId,
            StatusId = request.StatusId,
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

        _logger.LogInformation("Expiring subscription with ID: {Id}", id);

        try
        {
            var existingSubscription = await GetSubscriptionByIdAsync(id, cancellationToken);
            if (existingSubscription == null)
            {
                _logger.LogWarning("Subscription with ID {Id} not found for expiration", id);
                return false;
            }

            // Mark as expired
            existingSubscription.StatusId = Status.Expired;
            existingSubscription.lastUpdated = DateTime.UtcNow;
            existingSubscription.subscriptionName = $"Expired-{existingSubscription.subscriptionName ?? id.ToString()}";

            var actor = GetActorProxy(id);
            var result = await actor.FastSetDataAsync(existingSubscription, cancellationToken);

            _logger.LogInformation("Subscription with ID {Id} marked as expired.", id);
            return result;
        }
        catch (KeyNotFoundException knfEx)
        {
            _logger.LogWarning(knfEx, "Key not found for subscription ID: {Id}", id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error expiring subscription with ID: {Id}", id);
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

            if (dto != null && dto.Id == subscriptionId && dto.StatusId != Status.Expired)
            {
                return dto;
            }
        }

        return null;
    }

    public async Task<Guid> CreatePaymentAsync(
       PaymentTransactionRequestDto request,
       Guid userId,
       CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        // 🔍 Check for existing active subscription
        foreach (var existingId in _paymentTransactionIds.Keys)
        {
            var existingActor = GetPaymentTransactionActorProxy(existingId);
            var existingPayment = await existingActor.GetDataAsync(cancellationToken);

            if (existingPayment != null &&
                existingPayment.SubscriptionId == request.SubscriptionId &&
                existingPayment.UserId == userId &&
                existingPayment.EndDate > DateTime.UtcNow)
            {
                throw new InvalidOperationException("You already have an active subscription for this package.");
            }
        }

        var id = Guid.NewGuid();
        var startDate = DateTime.UtcNow;

        var subscriptionActor = GetActorProxy(request.SubscriptionId);
        var subscriptionData = await subscriptionActor.GetDataAsync(cancellationToken);

        if (subscriptionData == null)
            throw new Exception($"PayToPublish data not found for ID: {request.SubscriptionId}");

        var endDate = ParseDurationAndGetEndDate(startDate, subscriptionData.duration);

        var dto = new PaymentTransactionDto
        {
            Id = id,
            SubscriptionId = request.SubscriptionId,
            VerticalId = request.VerticalId,
            CategoryId = request.CategoryId,
            CardNumber = request.CardDetails.CardNumber,
            ExpiryMonth = request.CardDetails.ExpiryMonth,
            ExpiryYear = request.CardDetails.ExpiryYear,
            UserId = userId,
            StartDate = startDate,
            EndDate = endDate,
            CardHolderName = request.CardDetails.CardHolderName,
            TransactionDate = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow,
        };

        var actor = GetPaymentTransactionActorProxy(dto.Id);
        var result = await actor.FastSetDataAsync(dto, cancellationToken);

        if (!result)
            throw new Exception("Payment transaction creation failed.");

        _paymentTransactionIds.TryAdd(dto.Id, 0);
        _logger.LogInformation("Payment transaction created with ID: {TransactionId}", dto.Id);

        const string subscriberRole = "Subscriber";

        var role = await _roleManager.FindByNameAsync(subscriberRole);
        if (role == null)
        {
            var createRoleResult = await _roleManager.CreateAsync(new IdentityRole<Guid>(subscriberRole));
            if (!createRoleResult.Succeeded)
                throw new Exception("Failed to create Subscriber role.");
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            throw new Exception($"User not found with ID: {userId}");

        var currentRoles = await _userManager.GetRolesAsync(user);

        if (!currentRoles.Contains(subscriberRole))
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
                throw new Exception("Failed to remove existing roles.");

            var addResult = await _userManager.AddToRoleAsync(user, subscriberRole);
            if (!addResult.Succeeded)
                throw new Exception("Failed to add user to Subscriber role.");
        }

        return dto.Id;
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
   


}

