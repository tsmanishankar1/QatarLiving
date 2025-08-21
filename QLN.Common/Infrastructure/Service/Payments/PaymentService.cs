using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s;
using QLN.Common.DTO_s.Payments;
using QLN.Common.DTO_s.Payments.QLN.Common.DTO_s.Payments;
using QLN.Common.DTO_s.Subscription;
using QLN.Common.DTOs;
using QLN.Common.Infrastructure.IService.IAuth;
using QLN.Common.Infrastructure.IService.IPayments;
using QLN.Common.Infrastructure.IService.IProductService;
using QLN.Common.Infrastructure.Model;
using QLN.Common.Infrastructure.QLDbContext;
using QLN.Common.Infrastructure.Subscriptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.Service.Payments
{
    public class PaymentService : IPaymentService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentService> _logger;
        private readonly IFatoraService _fatoraService;
        private readonly ID365Service _d365Service;
        private readonly QLPaymentsContext _dbContext;
        private readonly QLSubscriptionContext _subscriptionContext;
        private readonly QLApplicationContext _applicationDbContext;
        private readonly IV2SubscriptionService _subscriptionService;

        public PaymentService(
            ILogger<PaymentService> logger,
            IFatoraService fatoraService,
            ID365Service d365Service,
            QLPaymentsContext dbContext,
            IConfiguration configuration,
            IV2SubscriptionService subscriptionService,
            QLSubscriptionContext subscriptionContext,
            QLApplicationContext applicationDbContext
            )
        {
            _logger = logger;
            _fatoraService = fatoraService;
            _d365Service = d365Service;
            _dbContext = dbContext;
            _configuration = configuration;
            _subscriptionService = subscriptionService;
            _subscriptionContext = subscriptionContext;
            _applicationDbContext = applicationDbContext;
        }

        public async Task<PaymentResponse> PayAsync(ExternalPaymentRequest request, CancellationToken cancellationToken = default)
        {
            var platform = "web";

            if (string.IsNullOrEmpty(request.User.UserName) || string.IsNullOrEmpty(request.User.UserId))
            {
                _logger.LogError("Username is null or empty");
                return new PaymentResponse
                {
                    Status = "failure",
                    Error = new FaturaPaymentError
                    {
                        ErrorCode = "400",
                        Description = "Bad Request: User info cannot be null or empty."
                    }
                };
            }

            if (string.IsNullOrEmpty(request.User.Email) && string.IsNullOrEmpty(request.User.Mobile))
            {
                _logger.LogError("Both Email and Mobile are null or empty");
                return new PaymentResponse
                {
                    Status = "failure",
                    Error = new FaturaPaymentError
                    {
                        ErrorCode = "400",
                        Description = "Bad Request: Email and Mobile cannot be both null or empty."
                    }
                };
            }

            if (request.ProductType == null)
            {
                _logger.LogError("ProductType is null");
                return new PaymentResponse
                {
                    Status = "failure",
                    Error = new FaturaPaymentError
                    {
                        ErrorCode = "400",
                        Description = "Bad Request: ProductType cannot be null."
                    }
                };
            }

            switch (request.ProductType)
            {
                case ProductType.SUBSCRIPTION:
                    _logger.LogInformation("Processing subscription payment for User ID: {UserId}", request.User.UserId);

                    var validationResult = await ValidateSubscriptionConstraintsAsync(request, cancellationToken);
                    if (!validationResult.IsValid)
                    {
                        _logger.LogWarning("Subscription validation failed for user {UserId}: {ErrorMessage}",
                            request.User.UserId, validationResult.ErrorMessage);
                        return new PaymentResponse
                        {
                            Status = "failure",
                            Error = new FaturaPaymentError
                            {
                                ErrorCode = "409", 
                                Description = validationResult.ErrorMessage
                            }
                        };
                    }

                    var dbResult = _dbContext.Payments.Add(new PaymentEntity
                    {
                        ProductType = request.ProductType ?? ProductType.FREE,
                        Status = PaymentStatus.Pending,
                        Fee = request.Amount ?? 0,
                        PaidByUid = request.User.UserId,
                        Date = DateTime.UtcNow,
                        Source = platform == "web" ? Source.Web : Source.Mobile,
                        Gateway = Gateway.FATORA,
                        Vertical = request.Vertical,
                        TriggeredSource = platform == "web" ? TriggeredSource.Web : TriggeredSource.Cron,
                    });

                    _dbContext.SaveChanges();

                    request.OrderId = dbResult.Entity.PaymentId;

                    _logger.LogInformation("Created PaymentEntity with ID: {PaymentId}", dbResult.Entity.PaymentId);

                    var subscriptionRequest = new V2SubscriptionPurchaseRequestDto
                    {
                        PaymentId = dbResult.Entity.PaymentId,
                        ProductCode = request.ProductCode,
                        UserId = request.User.UserId,
                    };

                    var subscriptionId = await _subscriptionService.PurchaseSubscriptionAsync(subscriptionRequest, cancellationToken);

                    dbResult.Entity.UserSubscriptionId = subscriptionId;

                    _dbContext.SaveChanges();

                    return await _fatoraService.CreatePaymentAsync(request, request.User.UserName, request.ProductCode, request.Vertical, request.SubVertical, request.User.Email, request.User.Mobile, platform, cancellationToken);

                case ProductType.PUBLISH:
                    _logger.LogInformation("Processing Pay to Publish payment for Order ID: {OrderId}", request.OrderId);
                    var publishDbResult = _dbContext.Payments.Add(new PaymentEntity
                    {
                        AdId = request.AdId,
                        SubVertical = request.SubVertical ?? null,
                        ProductType = request.ProductType ?? ProductType.FREE,
                        UserSubscriptionId = request.SubscriptionId,
                        Status = PaymentStatus.Pending,
                        Fee = request.Amount ?? 0,
                        PaidByUid = request.User.UserId,
                        Date = DateTime.UtcNow,
                        Source = platform == "web" ? Source.Web : Source.Mobile,
                        Gateway = Gateway.FATORA,
                        Vertical = request.Vertical,
                        TriggeredSource = platform == "web" ? TriggeredSource.Web : TriggeredSource.Cron,
                    });

                    _dbContext.SaveChanges();

                    request.OrderId = publishDbResult.Entity.PaymentId;

                    _logger.LogInformation("Created PaymentEntity with ID: {PaymentId}", publishDbResult.Entity.PaymentId);

                    var publishRequest = new V2SubscriptionPurchaseRequestDto
                    {
                        PaymentId = publishDbResult.Entity.PaymentId,
                        ProductCode = request.ProductCode,
                        UserId = request.User.UserId,
                        AdId = request.AdId,
                    };

                    var pubsubscriptionId = await _subscriptionService.PurchaseSubscriptionAsync(publishRequest, cancellationToken);

                    publishDbResult.Entity.UserSubscriptionId = pubsubscriptionId;

                    _dbContext.SaveChanges();

                    return await _fatoraService.CreatePaymentAsync(request, request.User.UserName, request.ProductCode, request.Vertical, request.SubVertical, request.User.Email, request.User.Mobile, platform, cancellationToken);

                case ProductType.ADDON_COMBO:
                case ProductType.ADDON_FEATURE:
                case ProductType.ADDON_REFRESH:
                case ProductType.ADDON_PROMOTE:
                    _logger.LogInformation("Processing Addon payment for Order ID: {OrderId}", request.OrderId);
                    var addonDbResult = _dbContext.Payments.Add(new PaymentEntity
                    {
                        AdId = request.AdId,
                        ProductType = request.ProductType ?? ProductType.FREE,
                        Status = PaymentStatus.Pending,
                        UserSubscriptionId = request.SubscriptionId,
                        Fee = request.Amount ?? 0,
                        PaidByUid = request.User.UserId,
                        Date = DateTime.UtcNow,
                        Source = platform == "web" ? Source.Web : Source.Mobile,
                        Gateway = Gateway.FATORA,
                        Vertical = request.Vertical,
                        SubVertical = request.SubVertical,
                        TriggeredSource = platform == "web" ? TriggeredSource.Web : TriggeredSource.Cron,
                    });

                    _dbContext.SaveChanges();

                    request.OrderId = addonDbResult.Entity.PaymentId;

                    _logger.LogInformation("Created PaymentEntity with ID: {PaymentId}", addonDbResult.Entity.PaymentId);

                    var addonRequest = new V2UserAddonPurchaseRequestDto
                    {
                        PaymentId = addonDbResult.Entity.PaymentId,
                        ProductCode = request.ProductCode,
                        UserId = request.User.UserId,
                        SubscriptionId = request.SubscriptionId ?? Guid.Empty,
                    };

                    var addonId = await _subscriptionService.PurchaseAddonAsync(addonRequest, cancellationToken);

                    addonDbResult.Entity.UserAddonId = addonId;

                    _dbContext.SaveChanges();

                    return await _fatoraService.CreatePaymentAsync(request, request.User.UserName, request.ProductCode, request.Vertical, request.SubVertical, request.User.Email, request.User.Mobile, platform, cancellationToken);

                default:
                    _logger.LogError("Unsupported ProductType: {ProductType}", request.ProductType);
                    break;
            }

            return new PaymentResponse
            {
                Status = "failure",
                Error = new FaturaPaymentError
                {
                    ErrorCode = "400",
                    Description = "Bad Request"
                }
            };
        }

        /// <summary>
        /// Validates subscription constraints before allowing payment creation
        /// </summary>
        /// <param name="request">The payment request containing subscription details</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result indicating if the subscription can be created</returns>
        private async Task<SubscriptionValidationResult> ValidateSubscriptionConstraintsAsync(
            ExternalPaymentRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Validating subscription constraints for user {UserId}, vertical {Vertical}, subVertical {SubVertical}",
                    request.User.UserId, request.Vertical, request.SubVertical);

                var existingSubscriptions = await _subscriptionService.GetUserSubscriptionsAsync(
                    request.Vertical,
                    request.SubVertical,
                    request.User.UserId,
                    cancellationToken);

                var activeSubscriptions = existingSubscriptions
                    .Where(s => s.IsActive && s.EndDate > DateTime.UtcNow && s.ProductType == request.ProductType)
                    .ToList();

                _logger.LogDebug("Found {Count} active subscriptions for user {UserId}",
                    activeSubscriptions.Count, request.User.UserId);

                switch (request.Vertical)
                {
                    case Vertical.Classifieds:
                        return ValidateClassifiedsSubscription(activeSubscriptions, request.SubVertical);

                    case Vertical.Services:
                        return ValidateServicesSubscription(activeSubscriptions);


                    default:
                        _logger.LogWarning("Unknown vertical type: {Vertical}", request.Vertical);
                        return SubscriptionValidationResult.Success();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating subscription constraints for user {UserId}", request.User.UserId);
                return SubscriptionValidationResult.Failure("Unable to validate subscription constraints. Please try again.");
            }
        }

        /// <summary>
        /// Validates Classifieds subscription - one active subscription per SubVertical
        /// </summary>
        private SubscriptionValidationResult ValidateClassifiedsSubscription(
            List<V2SubscriptionResponseDto> activeSubscriptions,
            SubVertical? requestedSubVertical)
        {
            if (!requestedSubVertical.HasValue)
            {
                return SubscriptionValidationResult.Failure("SubVertical is required for Classifieds subscriptions.");
            }

            // Check if user already has an active subscription for the same SubVertical
            var existingForSubVertical = activeSubscriptions
                .Where(s => s.SubVertical == requestedSubVertical.Value)
                .ToList();

            if (existingForSubVertical.Any())
            {
                var existingSub = existingForSubVertical.First();
                var daysRemaining = (int)(existingSub.EndDate - DateTime.UtcNow).TotalDays;

                _logger.LogWarning("User already has active Classifieds subscription for SubVertical {SubVertical}. " +
                    "Subscription ID: {SubscriptionId}, Days remaining: {DaysRemaining}",
                    requestedSubVertical, existingSub.Id, daysRemaining);

                return SubscriptionValidationResult.Failure(
                    $"You already have an active {requestedSubVertical} subscription with {daysRemaining} days remaining. " +
                    "Please wait for it to expire or cancel it before purchasing a new one.");
            }

            return SubscriptionValidationResult.Success();
        }

        /// <summary>
        /// Validates Services subscription - only one active subscription allowed
        /// </summary>
        private SubscriptionValidationResult ValidateServicesSubscription(List<V2SubscriptionResponseDto> activeSubscriptions)
        {
            if (activeSubscriptions.Any())
            {
                var existingSub = activeSubscriptions.First();
                var daysRemaining = (int)(existingSub.EndDate - DateTime.UtcNow).TotalDays;

                _logger.LogWarning("User already has active Services subscription. " +
                    "Subscription ID: {SubscriptionId}, Days remaining: {DaysRemaining}",
                    existingSub.Id, daysRemaining);

                return SubscriptionValidationResult.Failure(
                    $"You already have an active Services subscription with {daysRemaining} days remaining. " +
                    "Please wait for it to expire or cancel it before purchasing a new one.");
            }

            return SubscriptionValidationResult.Success();
        }

        public async Task<string> PaymentFailureAsync(PaymentTransactionRequest request, CancellationToken cancellationToken = default)
        {
            string baseRedirectUrl = GenerateRedirectURLBase(request.Vertical, request.SubVertical);

            if (!int.TryParse(request.OrderId, out var orderId))
                return $"{baseRedirectUrl}?paymentSuccess=false&error=invalid_order_id";

            _logger.LogDebug("Processing payment failure for Order ID: {OrderId}", orderId);

            try
            {
                var payment = await _dbContext.Payments
                    .FirstOrDefaultAsync(p => p.PaymentId == orderId || p.AttachedPaymentId == orderId, cancellationToken);

                var subscription = await _subscriptionContext.Subscriptions
                    .FirstOrDefaultAsync(s => s.PaymentId == orderId, cancellationToken);

                _logger.LogDebug("Retrieved payment: {PaymentId}, subscription: {SubscriptionId}",
                    payment?.PaymentId, subscription?.SubscriptionId);

                if (payment == null)
                {
                    _logger.LogError("Payment not found for Order ID: {OrderId}", orderId);
                    return $"{baseRedirectUrl}?paymentSuccess=false&error=payment_not_found";
                }

                payment.Status = PaymentStatus.Failure;
                payment.TransactionId = request.TransactionId;
                payment.TriggeredSource = TriggeredSource.Web;

                if (subscription != null)
                {
                    subscription.Status = SubscriptionStatus.PaymentFailed;
                    subscription.StartDate = DateTime.MinValue;
                    subscription.EndDate = DateTime.MinValue;

                    _logger.LogDebug("Subscription status updated to Failed for Order ID: {OrderId}", orderId);

                    _subscriptionContext.Update(subscription);
                    await _subscriptionContext.SaveChangesAsync(cancellationToken);
                }
                else
                {
                    _logger.LogWarning("No subscription found for failed payment Order ID: {OrderId}", orderId);
                }

                _dbContext.Update(payment);
                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogDebug("Payment status updated to Failure for Order ID: {OrderId}", orderId);

                var d365Data = new D365Data
                {
                    PaymentInfo = payment,
                    Operation = D365PaymentOperations.FAILURE,
                    User = new()
                    {
                        Id = payment.PaidByUid,
                        Email = string.Empty,
                        Mobile = string.Empty,
                        Name = string.Empty
                    }
                };

                await _d365Service.SendPaymentInfoD365Async(d365Data, cancellationToken);

                _logger.LogDebug("Payment failure information sent to D365 for Order ID: {OrderId}", orderId);

                return $"{baseRedirectUrl}?paymentSuccess=false&error=payment_failed";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment failure for Order ID: {OrderId}", orderId);
                return $"{baseRedirectUrl}?paymentSuccess=false&error=processing_error";
            }
        }

        public async Task<string> PaymentSuccessAsync(PaymentTransactionRequest request, CancellationToken cancellationToken = default)
        {
            string baseRedirectUrl = GenerateRedirectURLBase(request.Vertical, request.SubVertical);

            if (!int.TryParse(request.OrderId, out var orderId))
                return $"{baseRedirectUrl}?paymentSuccess=false";

            _logger.LogDebug("Processing payment success for Order ID: {OrderId}", orderId);

            var paymentConfirmation = await _fatoraService.VerifyPayment(request.OrderId, cancellationToken);

            if (paymentConfirmation == null)
            {
                _logger.LogError("Payment confirmation is null for Order ID: {OrderId}", request.OrderId);
                return $"{baseRedirectUrl}?paymentSuccess=false";
            }

            if (paymentConfirmation.Status != "SUCCESS")
            {
                _logger.LogError("Payment verification failed for Order ID: {OrderId}. Status: {Status}",
                request.OrderId, paymentConfirmation.Status);
                return $"{baseRedirectUrl}?paymentSuccess=false";
            }

            var payment = await _dbContext.Payments
                .FirstOrDefaultAsync(p => p.PaymentId == orderId || p.AttachedPaymentId == orderId, cancellationToken);

            var subscription = await _subscriptionContext.Subscriptions
                .FirstOrDefaultAsync(s => s.PaymentId == orderId, cancellationToken);

            var product = await _subscriptionContext.Products
                .FirstOrDefaultAsync(p => p.ProductCode == request.ProductCode, cancellationToken);

            _logger.LogDebug("Retrieved payment: {PaymentId}, subscription: {SubscriptionId}, product: {ProductCode}",
            payment?.PaymentId, subscription?.SubscriptionId, product?.ProductCode);

            if (payment == null)
            {
                _logger.LogError("Payment not found for Order ID: {OrderId}", orderId);
                return $"{baseRedirectUrl}?paymentSuccess=false";
            }

            if (subscription == null)
            {
                _logger.LogError("Subscription not found for Order ID: {OrderId}", orderId);
                return $"{baseRedirectUrl}?paymentSuccess=false";
            }

            if (product == null)
            {
                _logger.LogError("Product not found for ProductCode: {ProductCode}", request.ProductCode);
                return $"{baseRedirectUrl}?paymentSuccess=false";
            }

            try
            {
                payment.Status = PaymentStatus.Success;
                payment.TransactionId = paymentConfirmation.Result.TransactionId;
                payment.TriggeredSource = TriggeredSource.Web;
                _dbContext.Update(payment);
                await _dbContext.SaveChangesAsync(cancellationToken);

                // Use the IV2SubscriptionService to update the subscription status and dates
                await _subscriptionService.UpdateSubscriptionStatusAsync(subscription.SubscriptionId, SubscriptionStatus.Active, cancellationToken);

                // Update user subscription with retry logic for concurrency handling
                await UpdateUserSubscriptionAsync(subscription, orderId, cancellationToken);

                // Save subscription context changes
                await _subscriptionContext.SaveChangesAsync(cancellationToken);

                var d365Data = new D365Data
                {
                    PaymentInfo = payment,
                    Operation = D365PaymentOperations.SUCCESS,
                    User = new()
                    {
                        Id = payment.PaidByUid,
                        Email = string.Empty,
                        Mobile = string.Empty,
                        Name = string.Empty
                    }
                };

                await _d365Service.SendPaymentInfoD365Async(d365Data, cancellationToken);

                _logger.LogDebug("Payment information sent to D365 for Order ID: {OrderId}", orderId);

                return $"{baseRedirectUrl}?paymentSuccess=true";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating payment and subscription for Order ID: {OrderId}", orderId);
                return $"{baseRedirectUrl}?paymentSuccess=false";
            }
        }

        private async Task UpdateUserSubscriptionAsync(Subscription subscription, int orderId, CancellationToken cancellationToken)
        {
            try
            {
                var userId = Guid.Parse(subscription.UserId);

                var existingUserSubscription = await _applicationDbContext.UserSubscriptions
                    .FirstOrDefaultAsync(us => us.Id == subscription.SubscriptionId, cancellationToken);

                if (existingUserSubscription != null)
                {
                    existingUserSubscription.DisplayName = subscription.ProductName;
                    existingUserSubscription.ProductCode = subscription.ProductCode;
                    existingUserSubscription.ProductName = subscription.ProductName;
                    existingUserSubscription.Vertical = subscription.Vertical;
                    existingUserSubscription.SubVertical = subscription.SubVertical;
                    existingUserSubscription.StartDate = subscription.StartDate;
                    existingUserSubscription.EndDate = subscription.EndDate;

                    _applicationDbContext.UserSubscriptions.Update(existingUserSubscription);

                    _logger.LogDebug("Updated existing user subscription {SubscriptionId} for user {UserId}",
                        subscription.SubscriptionId, subscription.UserId);
                }
                else
                {
                    var userSubscription = new UserSubscription
                    {
                        UserId = userId,
                        Id = subscription.SubscriptionId,
                        DisplayName = subscription.ProductName,
                        ProductCode = subscription.ProductCode,
                        ProductName = subscription.ProductName,
                        Vertical = subscription.Vertical,
                        SubVertical = subscription.SubVertical,
                        StartDate = subscription.StartDate,
                        EndDate = subscription.EndDate
                    };

                    _applicationDbContext.UserSubscriptions.Add(userSubscription);

                    _logger.LogDebug("Added new user subscription {SubscriptionId} for user {UserId}",
                        subscription.SubscriptionId, subscription.UserId);
                }

                await _applicationDbContext.SaveChangesAsync(cancellationToken);
                _logger.LogDebug("User subscription updated successfully for Order ID: {OrderId}", orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user subscription for Order ID: {OrderId}", orderId);
                throw;
            }
        }
        private string GenerateRedirectURLBase(Vertical vertical, SubVertical? subVertical)
        {
            var baseUrls = _configuration.GetSection("BaseUrl").GetChildren()
                                         .ToDictionary(x => x.Key, x => x.Value);

            string baseRedirectUrl = string.Empty;

            if (baseUrls.TryGetValue(vertical.ToString(), out var verticalBaseUrl))
            {
                if (vertical == Vertical.Classifieds && subVertical.HasValue)
                {
                    var subVerticalKey = subVertical.ToString();
                    var classifiedBaseUrls = _configuration.GetSection("BaseUrl:Classifieds").GetChildren()
                                                          .ToDictionary(x => x.Key, x => x.Value);
                    if (classifiedBaseUrls.TryGetValue(subVerticalKey, out var subVerticalUrl))
                    {
                        return subVerticalUrl;
                    }
                    else
                    {
                        baseRedirectUrl = classifiedBaseUrls.GetValueOrDefault("Default", verticalBaseUrl);
                    }
                }
                else if (vertical == Vertical.Services)
                {
                    var servicesBaseUrls = _configuration.GetSection("BaseUrl:Services").Value;
                    baseRedirectUrl = servicesBaseUrls;
                }
            }
            else
            {
                baseRedirectUrl = _configuration.GetSection("BaseUrl")["LegacyDrupal"] ?? "https://default-legacy-url.com";
            }

            return baseRedirectUrl;
        }
    }

    /// <summary>
    /// Result of subscription validation
    /// </summary>
    public class SubscriptionValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;

        public static SubscriptionValidationResult Success() => new() { IsValid = true };

        public static SubscriptionValidationResult Failure(string errorMessage) => new()
        {
            IsValid = false,
            ErrorMessage = errorMessage
        };
    }
}