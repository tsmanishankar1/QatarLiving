using Dapr.Actors;
using Dapr.Actors.Client;
using Microsoft.AspNetCore.Identity;
using QLN.Common.DTO_s;
using QLN.Common.DTOs;
using QLN.Common.Infrastructure.IService.ISubscriptionService;
using QLN.Common.Infrastructure.Model;
using QLN.Common.Infrastructure.Subscriptions;
using System.Collections.Concurrent;

public class ExternalSubscriptionService : IExternalSubscriptionService
{
    private readonly ILogger<ExternalSubscriptionService> _logger;
    private static readonly ConcurrentDictionary<Guid, byte> _paymentTransactionIds = new();
    private static readonly ConcurrentDictionary<Guid, byte> _subscriptionIds = new();
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
         int? categoryId,
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
                var duration = data.Duration; // Already a TimeSpan

                resultList.Add(new SubscriptionResponseDto
                {
                    Id = data.Id,
                    SubscriptionName = data.subscriptionName,
                    DurationName = DurationConverter.ConvertToReadableFormat(duration),
                    Price = data.price,
                    Currency = data.currency,
                    AdsBudget=data.adsbudget,
                    FeatureBudget=data.featurebudget,
                    PromoteBudget=data.promotebudget,
                    RefreshBudget=data.refreshbudget,
                    Description = data.description
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


    public static class DurationConverter
    {
        public static string ConvertToReadableFormat(TimeSpan duration)
        {
            var totalDays = duration.TotalDays;

            return totalDays switch
            {
                90 => "3 Months",
                180 => "6 Months",
                365 => "1 Year",
                _ => FormatMonths((int)(totalDays / 30))
            };
        }

        private static string FormatMonths(int months)
        {
            return months == 1 ? "1 Month" : $"{months} Months";
        }
    }


    public async Task CreateSubscriptionAsync(SubscriptionRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var id = Guid.NewGuid();

        var dto = new SubscriptionDto
        {
            Id = id,
            subscriptionName = request.SubscriptionName,
            Duration = request.Duration,
            price = request.Price,
            description = request.Description,
            currency = request.Currency,
            adsbudget = request.adsbudget,
            promotebudget = request.promotebudget,
            refreshbudget = request.refreshbudget,
            featurebudget=request.featurebudget,
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

            // result.Duration is TimeSpan
            var readableDuration = ConvertTimeSpanToReadableString(result.Duration);

            subscriptions.Add(new SubscriptionResponseDto
            {
                Id = result.Id,
                SubscriptionName = result.subscriptionName,
                DurationName = readableDuration,
                Price = result.price,
                Description = result.description,
                Currency = result.currency
            });
        }

        return subscriptions;
    }
    private string ConvertTimeSpanToReadableString(TimeSpan duration)
    {
        var totalDays = (int)duration.TotalDays;

        return totalDays switch
        {
            90 => "3 Months",
            180 => "6 Months",
            365 => "1 Year",
            _ => $"{totalDays} Days"
        };
    }
    public async Task<bool> UpdateSubscriptionAsync(Guid subscriptionId, SubscriptionRequestDto request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var dto = new SubscriptionDto
        {
            Id = subscriptionId,
            subscriptionName = request.SubscriptionName,
            Duration = request.Duration,
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


    // Modified CreatePaymentAsync method
    public async Task<Guid> CreatePaymentAsync(
        PaymentTransactionRequestDto request,
        string userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var id = Guid.NewGuid();
        var startDate = DateTime.UtcNow;
        DateTime? existingEndDate = null;

        // Check for existing active subscriptions and determine the latest end date
        foreach (var existingId in _paymentTransactionIds.Keys)
        {
            var existingActor = GetPaymentTransactionActorProxy(existingId);
            var existingPayment = await existingActor.GetDataAsync(cancellationToken);

            if (existingPayment != null && existingPayment.UserId == userId)
            {
                if (existingEndDate == null || existingPayment.EndDate > existingEndDate)
                {
                    existingEndDate = existingPayment.EndDate;
                }
            }
        }

        // If there's an existing subscription, set new one to start after it
        if (existingEndDate.HasValue)
        {
            startDate = existingEndDate.Value;
            _logger.LogInformation("Found existing records for userId {UserId}. New subscription will start on {StartDate}",
                userId, startDate);
        }

        var subscriptionActor = GetActorProxy(request.SubscriptionId);
        var subscriptionData = await subscriptionActor.GetDataAsync(cancellationToken)
            ?? throw new Exception($"Subscription data not found for ID: {request.SubscriptionId}");

        // Calculate end date using TimeSpan
        var duration = subscriptionData.Duration; // TimeSpan
        var endDate = startDate.Add(duration);

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
            IsExpired = false
        };

        var actor = GetPaymentTransactionActorProxy(dto.Id);
        var result = await actor.FastSetDataAsync(dto, cancellationToken);

        if (!result)
            throw new Exception("Payment transaction creation failed.");

        // Store payment details for quick retrieval
        var durationEnum = MapTimeSpanToDurationType(subscriptionData.Duration);
        var paymentDetails = new UserPaymentDetailsResponseDto
        {
            UserId = userId,
            PaymentTransactionId = dto.Id,
            TransactionDate = dto.TransactionDate,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            SubscriptionId = subscriptionData.Id,
            SubscriptionName = subscriptionData.subscriptionName,
            Price = subscriptionData.price,
            Currency = subscriptionData.currency,
            Description = subscriptionData.description,
            DurationId = (int)durationEnum,
            DurationName = durationEnum.ToString(),
            VerticalTypeId = (int)subscriptionData.VerticalTypeId,
            VerticalName = subscriptionData.VerticalTypeId.ToString(),
            CategoryId = (int)subscriptionData.CategoryId,
            CategoryName = subscriptionData.CategoryId.ToString(),
            AdsBudgetTotal = subscriptionData.adsbudget,
            PromoteBudgetTotal = subscriptionData.promotebudget,
            RefreshBudgetTotal = subscriptionData.refreshbudget,
            CardHolderName = dto.CardHolderName
        };

        // Store payment details in actor state
        await actor.StorePaymentDetailsAsync(paymentDetails, cancellationToken);

        _paymentTransactionIds.TryAdd(dto.Id, 0);

        _logger.LogInformation("Payment transaction created with ID: {TransactionId}, Start Date: {StartDate}, End Date: {EndDate}",
            dto.Id, dto.StartDate, dto.EndDate);

        if (!existingEndDate.HasValue)
        {
            // await AssignSubscriberRoleAsync(userId);
            _logger.LogInformation("Assigned subscriber role immediately for user {UserId} as this is their first subscription", userId);
        }
        else
        {
            _logger.LogInformation("Subscription extended for user {UserId}. Role assignment will continue from existing subscription", userId);
        }

        return dto.Id;
    }
    private async Task AssignSubscriberRoleAsync(string userId)
    {
        const string subscriberRole = "Subscriber";

        var role = await _roleManager.FindByNameAsync(subscriberRole);
        if (role == null)
        {
            var createRoleResult = await _roleManager.CreateAsync(new IdentityRole<Guid>(subscriberRole));
            if (!createRoleResult.Succeeded)
                throw new Exception("Failed to create Subscriber role.");
        }

        var user = await _userManager.FindByIdAsync(userId.ToString()) ?? throw new Exception($"User not found with ID: {userId}");

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
    }
    public async Task<string[]> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("[UserManagementService] Getting roles for user {UserId}", userId);

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                _logger.LogWarning("[UserManagementService] User not found with ID: {UserId}", userId);
                return Array.Empty<string>();
            }

            var roles = await _userManager.GetRolesAsync(user);
            return roles.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserManagementService] Error getting roles for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> AddUserToRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("[UserManagementService] Adding user {UserId} to role {Role}", userId, roleName);

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                _logger.LogWarning("[UserManagementService] User not found with ID: {UserId}", userId);
                return false;
            }

            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                var createRoleResult = await _roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
                if (!createRoleResult.Succeeded)
                {
                    _logger.LogError("[UserManagementService] Failed to create role {Role}", roleName);
                    return false;
                }
            }

            var result = await _userManager.AddToRoleAsync(user, roleName);
            if (result.Succeeded)
            {
                _logger.LogInformation("[UserManagementService] Successfully added user {UserId} to role {Role}", userId, roleName);
                return true;
            }

            _logger.LogError("[UserManagementService] Failed to add user {UserId} to role {Role}: {Errors}",
                userId, roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserManagementService] Error adding user {UserId} to role {Role}", userId, roleName);
            throw;
        }
    }

    public async Task<bool> RemoveUserFromRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("[UserManagementService] Removing user {UserId} from role {Role}", userId, roleName);

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                _logger.LogWarning("[UserManagementService] User not found with ID: {UserId}", userId);
                return false;
            }

            var result = await _userManager.RemoveFromRoleAsync(user, roleName);
            if (result.Succeeded)
            {
                _logger.LogInformation("[UserManagementService] Successfully removed user {UserId} from role {Role}", userId, roleName);
                return true;
            }

            _logger.LogError("[UserManagementService] Failed to remove user {UserId} from role {Role}: {Errors}",
                userId, roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserManagementService] Error removing user {UserId} from role {Role}", userId, roleName);
            throw;
        }
    }

    public async Task<bool> IsUserInRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("=== ROLE CHECK === Checking if user {UserId} is in role {Role}", userId, roleName);

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                _logger.LogWarning("=== ROLE CHECK === User not found with ID: {UserId}", userId);
                return false;
            }

            var result = await _userManager.IsInRoleAsync(user, roleName);
            _logger.LogInformation("=== ROLE CHECK === User {UserId} in role {Role}: {Result}", userId, roleName, result);

            var allRoles = await _userManager.GetRolesAsync(user);
            _logger.LogInformation("=== ROLE CHECK === User {UserId} all current roles: [{Roles}]", userId, string.Join(", ", allRoles));

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "=== ROLE CHECK ERROR === Error checking if user {UserId} is in role {Role}", userId, roleName);
            throw;
        }
    }

    public async Task<bool> ChangeUserRoleAsync(Guid userId, string newRole, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("[UserManagementService] Changing role for user {UserId} to {NewRole}", userId, newRole);

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                _logger.LogWarning("[UserManagementService] User not found with ID: {UserId}", userId);
                return false;
            }

            var roleExists = await _roleManager.RoleExistsAsync(newRole);
            if (!roleExists)
            {
                var createRoleResult = await _roleManager.CreateAsync(new IdentityRole<Guid>(newRole));
                if (!createRoleResult.Succeeded)
                {
                    _logger.LogError("[UserManagementService] Failed to create role {Role}: {Errors}",
                        newRole, string.Join(", ", createRoleResult.Errors.Select(e => e.Description)));
                    return false;
                }
                _logger.LogInformation("[UserManagementService] Created new role: {Role}", newRole);
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            _logger.LogInformation("[UserManagementService] User {UserId} current roles: {Roles}",
                userId, string.Join(", ", currentRoles));

            if (currentRoles.Contains(newRole))
            {
                _logger.LogInformation("[UserManagementService] User {UserId} already has role {Role}", userId, newRole);
                return true;
            }

            if (currentRoles.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    _logger.LogError("[UserManagementService] Failed to remove user {UserId} from current roles: {Errors}",
                        userId, string.Join(", ", removeResult.Errors.Select(e => e.Description)));
                    return false;
                }
                _logger.LogInformation("[UserManagementService] Removed user {UserId} from roles: {Roles}",
                    userId, string.Join(", ", currentRoles));
            }

            var addResult = await _userManager.AddToRoleAsync(user, newRole);
            if (addResult.Succeeded)
            {
                _logger.LogInformation("[UserManagementService] Successfully added user {UserId} to role {Role}", userId, newRole);
                return true;
            }
            else
            {
                _logger.LogError("[UserManagementService] Failed to add user {UserId} to role {Role}: {Errors}",
                    userId, newRole, string.Join(", ", addResult.Errors.Select(e => e.Description)));
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserManagementService] Error changing role for user {UserId} to {NewRole}", userId, newRole);
            throw;
        }
    }


    public async Task HandleSubscriptionExpiryAsync(SubscriptionExpiryMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("=== HANDLING SUBSCRIPTION EXPIRY === Starting process for user {UserId}, subscription {SubscriptionId}, payment {PaymentId}",
                message.UserId, message.SubscriptionId, message.PaymentTransactionId);

            // Validate the message
            if (message.UserId == string.Empty)
            {
                _logger.LogError("=== SUBSCRIPTION EXPIRY ERROR === Invalid UserId in message");
                return;
            }

            // Check if user exists first
            var user = await _userManager.FindByIdAsync(message.UserId.ToString());
            if (user == null)
            {
                _logger.LogError("=== SUBSCRIPTION EXPIRY ERROR === User not found with ID: {UserId}", message.UserId);
                return;
            }

            _logger.LogInformation("=== SUBSCRIPTION EXPIRY === Found user {UserId}, checking current roles", message.UserId);

            // Get current user roles
            var currentRoles = await _userManager.GetRolesAsync(user);
            _logger.LogInformation("=== SUBSCRIPTION EXPIRY === User {UserId} current roles: [{Roles}]",
                message.UserId, string.Join(", ", currentRoles));

            // Check if user is currently a subscriber
            bool isSubscriber = currentRoles.Contains("Subscriber");

            if (isSubscriber)
            {
                _logger.LogInformation("=== SUBSCRIPTION EXPIRY === User {UserId} is a Subscriber, changing role to User", message.UserId);

                // Remove from Subscriber role
                var removeResult = await _userManager.RemoveFromRoleAsync(user, "Subscriber");
                if (!removeResult.Succeeded)
                {
                    _logger.LogError("=== SUBSCRIPTION EXPIRY ERROR === Failed to remove Subscriber role from user {UserId}: {Errors}",
                        message.UserId, string.Join(", ", removeResult.Errors.Select(e => e.Description)));
                    return;
                }

                _logger.LogInformation("=== SUBSCRIPTION EXPIRY === Successfully removed Subscriber role from user {UserId}", message.UserId);

                // Ensure User role exists
                const string userRole = "User";
                var role = await _roleManager.FindByNameAsync(userRole);
                if (role == null)
                {
                    _logger.LogInformation("=== SUBSCRIPTION EXPIRY === Creating User role as it doesn't exist");
                    var createRoleResult = await _roleManager.CreateAsync(new IdentityRole<Guid>(userRole));
                    if (!createRoleResult.Succeeded)
                    {
                        _logger.LogError("=== SUBSCRIPTION EXPIRY ERROR === Failed to create User role: {Errors}",
                            string.Join(", ", createRoleResult.Errors.Select(e => e.Description)));
                        return;
                    }
                }

                // Add to User role
                var addResult = await _userManager.AddToRoleAsync(user, userRole);
                if (!addResult.Succeeded)
                {
                    _logger.LogError("=== SUBSCRIPTION EXPIRY ERROR === Failed to add User role to user {UserId}: {Errors}",
                        message.UserId, string.Join(", ", addResult.Errors.Select(e => e.Description)));
                    return;
                }

                _logger.LogInformation("=== SUBSCRIPTION EXPIRY === Successfully added User role to user {UserId}", message.UserId);

                // Verify the role change
                var updatedRoles = await _userManager.GetRolesAsync(user);
                _logger.LogInformation("=== SUBSCRIPTION EXPIRY === User {UserId} updated roles: [{Roles}]",
                    message.UserId, string.Join(", ", updatedRoles));

                // Remove from payment tracking if exists
                if (_paymentTransactionIds.ContainsKey(message.PaymentTransactionId))
                {
                    _paymentTransactionIds.TryRemove(message.PaymentTransactionId, out _);
                    _logger.LogInformation("=== SUBSCRIPTION EXPIRY === Removed expired payment transaction {PaymentTransactionId} from tracking",
                        message.PaymentTransactionId);
                }

                _logger.LogInformation("=== SUBSCRIPTION EXPIRY COMPLETED === Successfully processed expiry for user {UserId}", message.UserId);
            }
            else
            {
                _logger.LogInformation("=== SUBSCRIPTION EXPIRY === User {UserId} is not currently a subscriber (roles: [{Roles}]), no role change needed",
                    message.UserId, string.Join(", ", currentRoles));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "=== SUBSCRIPTION EXPIRY CRITICAL ERROR === Error handling subscription expiry for user {UserId}", message.UserId);
            throw;
        }
    }


    public async Task ProcessExpiredSubscriptionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("[UserManagementService] Processing expired subscriptions manually");

            var expiredPaymentIds = new List<Guid>();

            foreach (var paymentId in _paymentTransactionIds.Keys)
            {
                try
                {
                    var actor = GetPaymentTransactionActorProxy(paymentId);
                    var paymentData = await actor.GetDataAsync(cancellationToken);

                    if (paymentData != null && paymentData.EndDate <= DateTime.UtcNow)
                    {
                        expiredPaymentIds.Add(paymentId);


                        var expiryMessage = new SubscriptionExpiryMessage
                        {
                            UserId = paymentData.UserId,
                            SubscriptionId = paymentData.SubscriptionId,
                            PaymentTransactionId = paymentData.Id,
                            ExpiryDate = paymentData.EndDate,
                            ProcessedAt = DateTime.UtcNow
                        };

                        await HandleSubscriptionExpiryAsync(expiryMessage, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[UserManagementService] Error processing payment transaction {PaymentId}", paymentId);
                }
            }

            _logger.LogInformation("[UserManagementService] Processed {Count} expired subscriptions", expiredPaymentIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserManagementService] Error processing expired subscriptions");
            throw;
        }


    }
    private DurationType MapTimeSpanToDurationType(TimeSpan duration)
    {
        var days = (int)duration.TotalDays;

        return days switch
        {
            90 => DurationType.ThreeMonths,
            180 => DurationType.SixMonths,
            365 => DurationType.OneYear,
            _ => throw new ArgumentOutOfRangeException(nameof(duration), $"Unsupported duration: {days} days")
        };
    }

    private IPaymentTransactionActor GetPaymentTransactionActorProxy(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Actor ID cannot be empty", nameof(id));

        return ActorProxy.Create<IPaymentTransactionActor>(
            new ActorId(id.ToString()),
            "PaymentTransactionActor");
    }
    public async Task<List<UserPaymentDetailsResponseDto>> GetUserPaymentDetailsAsync(
    string userId,
    CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting payment details for user {UserId}", userId);

            var userPaymentDetails = new List<UserPaymentDetailsResponseDto>();
            var paymentIds = _paymentTransactionIds.Keys.ToList();

            if (paymentIds.Count == 0)
            {
                _logger.LogWarning("No payment transactions found in tracking dictionary");
                return userPaymentDetails;
            }

            _logger.LogInformation("Found {Count} payment IDs to check for user {UserId}", paymentIds.Count, userId);


            var userPaymentTasks = paymentIds.Select(async paymentId =>
            {
                try
                {
                    var paymentActor = GetPaymentTransactionActorProxy(paymentId);
                    var paymentData = await paymentActor.GetDataAsync(cancellationToken);

                    if (paymentData != null && paymentData.UserId == userId)
                    {
                        return paymentData;
                    }
                    return null;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving payment data for ID: {PaymentId}", paymentId);
                    return null;
                }
            });

            var userPayments = (await Task.WhenAll(userPaymentTasks))
                .Where(p => p != null)
                .ToList();

            _logger.LogInformation("Found {Count} payments for user {UserId}", userPayments.Count, userId);


            foreach (var payment in userPayments)
            {
                try
                {
                    var subscriptionActor = GetActorProxy(payment.SubscriptionId);
                    var subscriptionData = await subscriptionActor.GetDataAsync(cancellationToken);

                    if (subscriptionData != null)
                    {
                        var durationEnum = MapTimeSpanToDurationType(subscriptionData.Duration);
                        var isActive = payment.EndDate > DateTime.UtcNow;

                        // Remove from tracking if expired
                        if (!isActive && _paymentTransactionIds.ContainsKey(payment.Id))
                        {
                            _paymentTransactionIds.TryRemove(payment.Id, out _);
                            _logger.LogInformation("Removed expired payment {PaymentId} from tracking", payment.Id);
                        }

                        userPaymentDetails.Add(new UserPaymentDetailsResponseDto
                        {

                            PaymentTransactionId = payment.Id,
                            TransactionDate = payment.TransactionDate,
                            StartDate = payment.StartDate,
                            EndDate = payment.EndDate,
                            SubscriptionId = subscriptionData.Id,
                            SubscriptionName = subscriptionData.subscriptionName,
                            Price = subscriptionData.price,
                            Currency = subscriptionData.currency,
                            Description = subscriptionData.description,
                            DurationId = (int)durationEnum,
                            DurationName = durationEnum.ToString(),


                            VerticalTypeId = (int)subscriptionData.VerticalTypeId,
                            VerticalName = subscriptionData.VerticalTypeId.ToString(),
                            CategoryId = (int)subscriptionData.CategoryId,
                            CategoryName = subscriptionData.CategoryId.ToString(),
                            AdsBudgetTotal = subscriptionData.adsbudget,
                            PromoteBudgetTotal = subscriptionData.promotebudget,
                            RefreshBudgetTotal = subscriptionData.refreshbudget,



                        });
                    }
                    else
                    {
                        _logger.LogWarning("Subscription data not found for payment {PaymentId}, subscription {SubscriptionId}",
                            payment.Id, payment.SubscriptionId);

                        // Still add payment info even if subscription is not found
                        userPaymentDetails.Add(new UserPaymentDetailsResponseDto
                        {
                            PaymentTransactionId = payment.Id,
                            TransactionDate = payment.TransactionDate,
                            StartDate = payment.StartDate,
                            EndDate = payment.EndDate,

                            CardHolderName = payment.CardHolderName,
                            SubscriptionId = payment.SubscriptionId,
                            SubscriptionName = "Subscription Not Found",
                            VerticalTypeId = payment.VerticalId,
                            CategoryId = payment.CategoryId
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving subscription data for payment {PaymentId}, subscription {SubscriptionId}",
                        payment.Id, payment.SubscriptionId);
                }
            }


            userPaymentDetails = userPaymentDetails
                .OrderByDescending(p => p.TransactionDate)
                .ToList();

            _logger.LogInformation("Successfully retrieved {Count} payment details for user {UserId}",
                userPaymentDetails.Count, userId);

            return userPaymentDetails;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment details for user {UserId}", userId);
            throw;
        }
    }

    public async Task<YearlySubscriptionResponseDto?> CheckYearlySubscriptionAsync(
      string userId,
      CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Checking yearly subscription for user {UserId}", userId);

            var paymentIds = _paymentTransactionIds.Keys.ToList();

            if (paymentIds.Count == 0)
            {
                _logger.LogWarning("No payment transactions found in tracking dictionary");
                return null;
            }


            var userPaymentTasks = paymentIds.Select(async paymentId =>
            {
                try
                {
                    var paymentActor = GetPaymentTransactionActorProxy(paymentId);
                    var paymentData = await paymentActor.GetDataAsync(cancellationToken);

                    if (paymentData != null &&
                        paymentData.UserId == userId &&
                        paymentData.EndDate > DateTime.UtcNow)
                    {
                        return paymentData;
                    }
                    return null;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving payment data for ID: {PaymentId}", paymentId);
                    return null;
                }
            });

            var userActivePayments = (await Task.WhenAll(userPaymentTasks))
                .Where(p => p != null)
                .ToList();


            foreach (var payment in userActivePayments)
            {
                try
                {

                    var subscriptionDuration = payment.EndDate - payment.StartDate;
                    bool isYearlySubscription = subscriptionDuration.TotalDays >= 360 && subscriptionDuration.TotalDays <= 370; // Allow some tolerance

                    if (isYearlySubscription)
                    {

                        var subscriptionActor = GetActorProxy(payment.SubscriptionId);
                        var subscriptionData = await subscriptionActor.GetDataAsync(cancellationToken);

                        if (subscriptionData != null)
                        {
                            return new YearlySubscriptionResponseDto
                            {
                                UserId = userId,
                                IsRewardsYearlySubscription = true,
                                Price = subscriptionData.price,
                                Currency = subscriptionData.currency,
                                EndDate = payment.EndDate,
                                PaymentTransactionId = payment.Id,
                                SubscriptionId = payment.SubscriptionId
                            };
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking yearly subscription for payment {PaymentId}", payment.Id);
                }
            }


            return new YearlySubscriptionResponseDto
            {
                UserId = userId,
                IsRewardsYearlySubscription = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking yearly subscription for user {UserId}", userId);
            throw;
        }
    }


}




