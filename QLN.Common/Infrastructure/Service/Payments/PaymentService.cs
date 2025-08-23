using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s;
using QLN.Common.DTO_s.Payments;
using QLN.Common.DTO_s.Services;
using QLN.Common.DTO_s.Subscription;
using QLN.Common.DTOs;
using QLN.Common.Infrastructure.IService.IAuth;
using QLN.Common.Infrastructure.IService.IPayments;
using QLN.Common.Infrastructure.IService.IProductService;
using QLN.Common.Infrastructure.IService.IService;
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
        private readonly IServices _services;

        public PaymentService(
            ILogger<PaymentService> logger,
            IFatoraService fatoraService,
            ID365Service d365Service,
            QLPaymentsContext dbContext,
            IConfiguration configuration,
            IV2SubscriptionService subscriptionService,
            QLSubscriptionContext subscriptionContext,
            QLApplicationContext applicationDbContext,
            IServices services
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
            _services = services;
        }

        public async Task<PaymentResponse> PayAsync(ExternalPaymentRequest request, CancellationToken cancellationToken = default)
        {
            const string platform = "web";

            var validationResult = await ValidatePaymentRequestAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
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

            _logger.LogInformation("Processing multi-product payment for User ID: {UserId} with {ProductCount} products",
                request.User?.UserId, request.Products?.Count ?? 0);

            try
            {
                var totalAmount = request.Amount ?? await CalculateTotalAmountAsync(request.Products, cancellationToken);

                var paymentEntity = new PaymentEntity
                {
                    Vertical = request.Vertical,
                    SubVertical = request.SubVertical,
                    AdId = request.AdId,
                    Status = PaymentStatus.Pending,
                    Fee = totalAmount,
                    PaidByUid = request.User.UserId,
                    Date = DateTime.UtcNow,
                    Source = platform == "web" ? Source.Web : Source.Mobile,
                    Gateway = Gateway.FATORA,
                    TriggeredSource = platform == "web" ? TriggeredSource.Web : TriggeredSource.Cron,
                    Products = request.Products.Select(p => new ProductDetails
                    {
                        ProductType = p.ProductType,
                        ProductCode = p.ProductCode,
                        Price = p.UnitPrice ?? 0
                    }).ToList()
                };

                _dbContext.Payments.Add(paymentEntity);
                await _dbContext.SaveChangesAsync(cancellationToken);

                request.OrderId = paymentEntity.PaymentId;

                _logger.LogInformation("Created PaymentEntity with ID: {PaymentId}, D365 Order ID: {D365OrderId}",
                    paymentEntity.PaymentId, GenerateD365OrderId(paymentEntity));

                var userAddonIds = new List<Guid>();
                Guid? userSubscriptionId = null;

                var orderedProducts = request.Products
                    .OrderBy(p => p.ProductType == ProductType.SUBSCRIPTION || p.ProductType == ProductType.PUBLISH ? 0 : 1)
                    .ToList();

                bool bucketHasSubscription = orderedProducts.Any(p => p.ProductType == ProductType.SUBSCRIPTION || p.ProductType == ProductType.PUBLISH);
                bool bucketHasAddon = orderedProducts.Any(p => p.ProductType == ProductType.ADDON_COMBO
                                                            || p.ProductType == ProductType.ADDON_FEATURE
                                                            || p.ProductType == ProductType.ADDON_REFRESH
                                                            || p.ProductType == ProductType.ADDON_PROMOTE);

                if (bucketHasAddon && !bucketHasSubscription && (!request.SubscriptionId.HasValue || request.SubscriptionId == Guid.Empty))
                {
                    request.SubscriptionId = await ResolveSubscriptionIdForAddonsAsync(
                        request.Vertical,
                        request.SubVertical,
                        request.User.UserId,
                        request.AdId,
                        cancellationToken
                    );
                    _logger.LogInformation("Resolved subscription for addon-only purchase. SubscriptionId: {SubId}", request.SubscriptionId);
                }

                foreach (var product in orderedProducts)
                {
                    try
                    {
                        var result = await ProcessProductAsync(paymentEntity, product, request, cancellationToken);

                        if (result.SubscriptionId.HasValue)
                            userSubscriptionId = result.SubscriptionId.Value;

                        if (result.AddonId.HasValue)
                            userAddonIds.Add(result.AddonId.Value);

                        if ((product.ProductType == ProductType.SUBSCRIPTION || product.ProductType == ProductType.PUBLISH)
                            && userSubscriptionId.HasValue && userSubscriptionId.Value != Guid.Empty)
                        {
                            request.SubscriptionId = userSubscriptionId.Value;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error pre-provisioning product {ProductCode}", product.ProductCode);
                    }
                }

                paymentEntity.UserSubscriptionId = userSubscriptionId;
                paymentEntity.UserAddonIds = userAddonIds;

                _dbContext.Update(paymentEntity);
                await _dbContext.SaveChangesAsync(cancellationToken);

                var fatoraRequest = CreateFatoraRequest(request, paymentEntity, platform);
                var combinedProductCodes = string.Join(",", request.Products.Select(p => p.ProductCode));

                return await _fatoraService.CreatePaymentAsync(
                    fatoraRequest,
                    request.User.UserName,
                    combinedProductCodes,
                    request.Vertical,
                    request.SubVertical,
                    request.User.Email,
                    request.User.Mobile,
                    platform,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing multi-product payment for User ID: {UserId}", request.User?.UserId);
                return new PaymentResponse
                {
                    Status = "failure",
                    Error = new FaturaPaymentError
                    {
                        ErrorCode = "500",
                        Description = "An internal error occurred while processing the payment"
                    }
                };
            }
        }

        private async Task<ProductProcessingResult> ProcessProductAsync(
            PaymentEntity payment,
            PaymentProductDto product,
            ExternalPaymentRequest request,
            CancellationToken cancellationToken)
        {
            switch (product.ProductType)
            {
                case ProductType.SUBSCRIPTION:
                    {
                        var dto = new V2SubscriptionPurchaseRequestDto
                        {
                            PaymentId = payment.PaymentId,
                            ProductCode = product.ProductCode,
                            UserId = payment.PaidByUid
                        };
                        var subscriptionId = await _subscriptionService.PurchaseSubscriptionAsync(dto, cancellationToken);
                        payment.UserSubscriptionId = subscriptionId;
                        _dbContext.Update(payment);
                        await _dbContext.SaveChangesAsync(cancellationToken);
                        return new ProductProcessingResult { SubscriptionId = subscriptionId };
                    }

                case ProductType.PUBLISH:
                    {
                        var dto = new V2SubscriptionPurchaseRequestDto
                        {
                            PaymentId = payment.PaymentId,
                            ProductCode = product.ProductCode,
                            UserId = payment.PaidByUid,
                            AdId = request.AdId,
                        };

                        var subscriptionId = await _subscriptionService.PurchaseSubscriptionAsync(dto, cancellationToken);
                        payment.UserSubscriptionId = subscriptionId;
                        _dbContext.Update(payment);
                        await _dbContext.SaveChangesAsync(cancellationToken);
                        return new ProductProcessingResult { SubscriptionId = subscriptionId };
                    }

                case ProductType.ADDON_COMBO:
                case ProductType.ADDON_FEATURE:
                case ProductType.ADDON_REFRESH:
                case ProductType.ADDON_PROMOTE:
                    {
                        var effectiveSubscriptionId = request.SubscriptionId;
                        if (!effectiveSubscriptionId.HasValue || effectiveSubscriptionId == Guid.Empty)
                        {
                            effectiveSubscriptionId = await ResolveSubscriptionIdForAddonsAsync(
                                payment.Vertical,
                                payment.SubVertical,
                                payment.PaidByUid,
                                payment.AdId,
                                cancellationToken
                            );
                        }

                        var dto = new V2UserAddonPurchaseRequestDto
                        {
                            PaymentId = payment.PaymentId,
                            ProductCode = product.ProductCode,
                            UserId = payment.PaidByUid,
                            SubscriptionId = effectiveSubscriptionId ?? Guid.Empty
                        };

                        var addonId = await _subscriptionService.PurchaseAddonAsync(dto, cancellationToken);
                        return new ProductProcessingResult { AddonId = addonId };
                    }

                default:
                    throw new ArgumentException($"Unsupported ProductType: {product.ProductType}");
            }
        }
        private async Task<SubscriptionValidationResult> ValidatePaymentRequestAsync(
            ExternalPaymentRequest request,
            CancellationToken cancellationToken)
        {
            var errors = new List<string>();

            if (request.User == null || string.IsNullOrWhiteSpace(request.User.UserId) || string.IsNullOrWhiteSpace(request.User.UserName))
                errors.Add("User information cannot be null or empty.");

            if (string.IsNullOrWhiteSpace(request.User?.Email) && string.IsNullOrWhiteSpace(request.User?.Mobile))
                errors.Add("Either Email or Mobile must be provided.");

            if (request.Products == null || request.Products.Count == 0)
                errors.Add("At least one product must be specified.");

            if (errors.Count > 0) return SubscriptionValidationResult.Failure(string.Join("; ", errors));

            var codes = request.Products.Select(p => p.ProductCode).Distinct().ToList();
            var master = await _subscriptionContext.Products
                .Where(p => codes.Contains(p.ProductCode))
                .Select(p => new
                {
                    p.ProductCode,
                    p.ProductType,
                    p.Vertical,
                    p.SubVertical,
                    p.Price
                })
                .ToListAsync(cancellationToken);

            var masterMap = master.ToDictionary(x => x.ProductCode, x => x, StringComparer.OrdinalIgnoreCase);

            foreach (var p in request.Products)
            {
                if (!masterMap.ContainsKey(p.ProductCode))
                    errors.Add($"Unknown product code: {p.ProductCode}");

                if (masterMap.TryGetValue(p.ProductCode, out var m))
                {
                    if (m.Vertical != request.Vertical)
                        errors.Add($"Product {p.ProductCode} belongs to {m.Vertical} and cannot be purchased under {request.Vertical}.");

                    if (m.SubVertical.HasValue && request.SubVertical.HasValue && m.SubVertical.Value != request.SubVertical.Value)
                        errors.Add($"Product {p.ProductCode} is restricted to {m.SubVertical}.");
                }
            }

            var subsCount = request.Products.Count(p => p.ProductType == ProductType.SUBSCRIPTION || p.ProductType == ProductType.PUBLISH);
            if (subsCount > 1)
                errors.Add("Only one subscription/publish product can be purchased per payment.");

            var hasAnyAddon = request.Products.Any(p =>
                p.ProductType == ProductType.ADDON_COMBO ||
                p.ProductType == ProductType.ADDON_FEATURE ||
                p.ProductType == ProductType.ADDON_REFRESH ||
                p.ProductType == ProductType.ADDON_PROMOTE);

            if (hasAnyAddon)
            {
                if (!request.AdId.HasValue)
                    errors.Add("Add-ons require a valid AdId.");

            }
            if (request.Products.Any(p => p.ProductType == ProductType.SUBSCRIPTION || p.ProductType == ProductType.PUBLISH))
            {
                var constraints = await ValidateSubscriptionConstraintsAsync(request, cancellationToken);
                if (!constraints.IsValid) errors.Add(constraints.ErrorMessage);
            }

            if (errors.Count > 0) return SubscriptionValidationResult.Failure(string.Join("; ", errors));
            return SubscriptionValidationResult.Success();
        }

        private async Task<SubscriptionValidationResult> ValidateSubscriptionConstraintsAsync(
            ExternalPaymentRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var existingSubscriptions = await _subscriptionService.GetUserSubscriptionsAsync(
                    request.Vertical,
                    request.SubVertical,
                    request.User.UserId,
                    cancellationToken);

                var activeSubscriptions = existingSubscriptions
                    .Where(s => s.IsActive && s.EndDate > DateTime.UtcNow && s.StatusId == SubscriptionStatus.Active)
                    .ToList();

                switch (request.Vertical)
                {
                    case Vertical.Classifieds:
                        return ValidateClassifiedsSubscription(activeSubscriptions, request.SubVertical);

                    case Vertical.Services:
                        return ValidateServicesSubscription(activeSubscriptions);

                    default:
                        return SubscriptionValidationResult.Success();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating subscription constraints for user {UserId}", request.User?.UserId);
                return SubscriptionValidationResult.Failure("Unable to validate subscription constraints. Please try again.");
            }
        }

        private SubscriptionValidationResult ValidateClassifiedsSubscription(
            List<V2SubscriptionResponseDto> activeSubscriptions,
            SubVertical? requestedSubVertical)
        {
            if (!requestedSubVertical.HasValue)
                return SubscriptionValidationResult.Failure("SubVertical is required for Classifieds subscriptions.");

            var existingForSubVertical = activeSubscriptions
                .Where(s => s.SubVertical == requestedSubVertical.Value && s.ProductType == ProductType.SUBSCRIPTION)
                .ToList();

            if (existingForSubVertical.Any())
            {
                var existingSub = existingForSubVertical.First();
                var daysRemaining = (int)(existingSub.EndDate - DateTime.UtcNow).TotalDays;

                return SubscriptionValidationResult.Failure(
                    $"You already have an active {requestedSubVertical} subscription with {daysRemaining} days remaining. " +
                    "You can buy a new plan to start after expiry or contact support to cancel the current one.");
            }

            return SubscriptionValidationResult.Success();
        }

        private SubscriptionValidationResult ValidateServicesSubscription(List<V2SubscriptionResponseDto> activeSubscriptions)
        {
            if (activeSubscriptions.Any())
            {
                var existingSub = activeSubscriptions.Where
                    (x=>x.ProductType == ProductType.SUBSCRIPTION).First();
                var daysRemaining = (int)(existingSub.EndDate - DateTime.UtcNow).TotalDays;

                return SubscriptionValidationResult.Failure(
                    $"You already have an active Services subscription with {daysRemaining} days remaining. " +
                    "You can queue a new plan to start after expiry or contact support to cancel the current one.");
            }

            return SubscriptionValidationResult.Success();
        }
        private async Task<decimal> CalculateTotalAmountAsync(List<PaymentProductDto> products, CancellationToken cancellationToken)
        {
            decimal total = 0;

            if (products == null || products.Count == 0) return 0;

            var codes = products.Select(p => p.ProductCode).Distinct().ToList();
            var master = await _subscriptionContext.Products
                .Where(p => codes.Contains(p.ProductCode))
                .Select(p => new { p.ProductCode, p.Price })
                .ToListAsync(cancellationToken);

            var priceMap = master.ToDictionary(x => x.ProductCode, x => x.Price, StringComparer.OrdinalIgnoreCase);

            foreach (var p in products)
            {
                if (p.UnitPrice.HasValue)
                {
                    total += p.UnitPrice.Value;
                }
                else if (priceMap.TryGetValue(p.ProductCode, out var dbPrice))
                {
                    total += dbPrice;
                    p.UnitPrice = dbPrice; 
                }
                else
                {
                    _logger.LogWarning("Product not found in database (no price): {ProductCode}", p.ProductCode);
                }
            }

            return total;
        }

        private string GenerateD365OrderId(PaymentEntity payment)
        {
            var prefix = "QLN";

            if (payment.Products != null && payment.Products.Any())
            {
                var firstProductCode = payment.Products.First().ProductCode ?? string.Empty;
                if (firstProductCode.StartsWith("QLC", StringComparison.OrdinalIgnoreCase)) prefix = "QLC";
                else if (firstProductCode.StartsWith("QLS", StringComparison.OrdinalIgnoreCase)) prefix = "QLS";
            }
            else
            {
                prefix = payment.Vertical switch
                {
                    Vertical.Classifieds => "QLC",
                    Vertical.Services => "QLS",
                    _ => "QLN"
                };
            }

            return $"{prefix}-{payment.PaymentId}";
        }

        private ExternalPaymentRequest CreateFatoraRequest(ExternalPaymentRequest originalRequest, PaymentEntity payment, string platform)
        {
            return new ExternalPaymentRequest
            {
                OrderId = payment.PaymentId,
                Amount = payment.Fee,
                User = originalRequest.User,
                Vertical = originalRequest.Vertical,
                SubVertical = originalRequest.SubVertical,
                Products = originalRequest.Products
            };
        }

        private static string BuildRedirect(string baseUrl, bool success, string? error = null)
        {
            var sep =baseUrl.Contains('?') ? "&" : "?";

            var qs = $"paymentSuccess={(success ? "true" : "false")}";
            if (!string.IsNullOrWhiteSpace(error))
                qs += $"&error={Uri.EscapeDataString(error)}";

            return $"{baseUrl}{sep}{qs}";
        }

        public async Task<string> PaymentFailureAsync(PaymentTransactionRequest request, CancellationToken cancellationToken = default)
        {
            string baseRedirectUrl = GenerateRedirectURLBase(request.Vertical, request.SubVertical);

            if (!int.TryParse(request.OrderId, out var orderId))
                return BuildRedirect(baseRedirectUrl, success: false, error: "invalid_order_id");

            _logger.LogDebug("Processing payment failure for Order ID: {OrderId}", orderId);

            try
            {
                var payment = await _dbContext.Payments
                    .Include(p => p.Products)
                    .FirstOrDefaultAsync(p => p.PaymentId == orderId || p.AttachedPaymentId == orderId, cancellationToken);

                if (payment == null)
                {
                    _logger.LogError("Payment not found for Order ID: {OrderId}", orderId);
                    return BuildRedirect(baseRedirectUrl, success: false, error: "payment_not_found");
                }

                payment.Status = PaymentStatus.Failure;
                payment.TransactionId = request.TransactionId;
                payment.TriggeredSource = TriggeredSource.Web;

                _dbContext.Update(payment);
                await _dbContext.SaveChangesAsync(cancellationToken);

                var subs = await _subscriptionContext.Subscriptions
                    .Where(s => s.PaymentId == orderId)
                    .ToListAsync(cancellationToken);

                foreach (var s in subs)
                {
                    s.Status = SubscriptionStatus.PaymentFailed;
                    s.StartDate = DateTime.MinValue;
                    s.EndDate = DateTime.MinValue;
                }

                if (subs.Count > 0)
                {
                    _subscriptionContext.UpdateRange(subs);
                    await _subscriptionContext.SaveChangesAsync(cancellationToken);
                }

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

                return BuildRedirect(baseRedirectUrl, success: false, error: "payment_failed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment failure for Order ID: {OrderId}", orderId);
                return BuildRedirect(baseRedirectUrl, success: false, error: "processing_error");
            }
        }

        public async Task<string> PaymentSuccessAsync(PaymentTransactionRequest request, CancellationToken cancellationToken = default)
        {
            string baseRedirectUrl = GenerateRedirectURLBase(request.Vertical, request.SubVertical);

            if (!int.TryParse(request.OrderId, out var orderId))
                return BuildRedirect(baseRedirectUrl, success: false);

            _logger.LogDebug("Processing payment success for Order ID: {OrderId}", orderId);

            var paymentConfirmation = await _fatoraService.VerifyPayment(request.OrderId, cancellationToken);
            if (paymentConfirmation == null || !string.Equals(paymentConfirmation.Status, "SUCCESS", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("Payment verification failed for Order ID {OrderId}. Status: {Status}",
                    request.OrderId, paymentConfirmation?.Status ?? "null");
                return BuildRedirect(baseRedirectUrl, success: false, error: "verification_failed");
            }

            var payment = await _dbContext.Payments
                .FirstOrDefaultAsync(p => p.PaymentId == orderId || p.AttachedPaymentId == orderId, cancellationToken);

            if (payment == null)
            {
                _logger.LogError("Payment not found for Order ID: {OrderId}", orderId);
                return BuildRedirect(baseRedirectUrl, success: false, error: "payment_not_found");
            }

            try
            {
                payment.Status = PaymentStatus.Success;
                payment.TransactionId = paymentConfirmation.Result.TransactionId;
                payment.TriggeredSource = TriggeredSource.Web;
                _dbContext.Update(payment);
                await _dbContext.SaveChangesAsync(cancellationToken);

                if (payment.UserSubscriptionId.HasValue && payment.UserSubscriptionId.Value != Guid.Empty)
                {
                    await _subscriptionService.UpdateSubscriptionStatusAsync(payment.UserSubscriptionId.Value, SubscriptionStatus.Active, cancellationToken);

                    var sub = await _subscriptionContext.Subscriptions.AsNoTracking()
                        .FirstOrDefaultAsync(s => s.SubscriptionId == payment.UserSubscriptionId.Value, cancellationToken);
                    if (sub != null)
                    {
                        await UpdateUserSubscriptionAsync(sub, orderId, cancellationToken);
                    }
                }

                if (payment.Vertical == Vertical.Services && payment.Products != null && payment.Products.Any())
                {
                    await ProcessServicesAddonsAsync(payment, cancellationToken);
                }

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

                return BuildRedirect(baseRedirectUrl, success: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating payment/subscriptions for Order ID: {OrderId}", orderId);
                return BuildRedirect(baseRedirectUrl, success: false, error: "processing_error");
            }
        }

        private async Task ProcessServicesAddonsAsync(PaymentEntity payment, CancellationToken cancellationToken)
        {
            try
            {
                if (!payment.AdId.HasValue || payment.AdId.Value <= 0)
                {
                    _logger.LogWarning("Cannot process services addons - AdId is missing or invalid for Payment ID: {PaymentId}", payment.PaymentId);
                    return;
                }

                var addonMappings = await GetAddonIdsByProductTypeAsync(payment, cancellationToken);

                foreach (var product in payment.Products)
                {
                    switch (product.ProductType)
                    {
                        case ProductType.ADDON_PROMOTE:
                            {
                                if (!addonMappings.TryGetValue(ProductType.ADDON_PROMOTE, out var addonId) || addonId == Guid.Empty)
                                {
                                    _logger.LogError("Cannot process P2Promote - AddonId for PROMOTE not found for Payment ID: {PaymentId}", payment.PaymentId);
                                    continue;
                                }

                                var promoteRequest = new PayToPromote
                                {
                                    ServiceId = payment.AdId.Value,
                                    AddonId = addonId,
                                };

                                await _services.P2PromoteService(promoteRequest, payment.PaidByUid, addonId, cancellationToken);
                                _logger.LogInformation("Successfully processed P2Promote for Service ID: {ServiceId}, AddonId: {AddonId}, Payment ID: {PaymentId}",
                                    payment.AdId.Value, addonId, payment.PaymentId);
                                break;
                            }

                        case ProductType.ADDON_FEATURE:
                            {
                                if (!addonMappings.TryGetValue(ProductType.ADDON_FEATURE, out var addonId) || addonId == Guid.Empty)
                                {
                                    _logger.LogError("Cannot process P2Feature - AddonId for FEATURE not found for Payment ID: {PaymentId}", payment.PaymentId);
                                    continue;
                                }

                                var featureRequest = new PayToFeature
                                {
                                    ServiceId = payment.AdId.Value,
                                    AddonId = addonId
                                };

                                await _services.P2FeatureService(featureRequest, payment.PaidByUid, addonId, cancellationToken);
                                _logger.LogInformation("Successfully processed P2Feature for Service ID: {ServiceId}, AddonId: {AddonId}, Payment ID: {PaymentId}",
                                    payment.AdId.Value, addonId, payment.PaymentId);
                                break;
                            }

                        case ProductType.PUBLISH:
                            {
                                var subscriptionId = payment.UserSubscriptionId ?? Guid.Empty;

                                if (subscriptionId == Guid.Empty)
                                {
                                    _logger.LogError("Cannot process P2Publish - SubscriptionId is missing for Payment ID: {PaymentId}", payment.PaymentId);
                                    continue;
                                }

                                var publishRequest = new PayToPublish
                                {
                                    ServiceId = payment.AdId.Value,
                                    SubscriptionId = subscriptionId
                                };

                                await _services.P2PublishService(publishRequest, payment.PaidByUid, subscriptionId, cancellationToken);
                                _logger.LogInformation("Successfully processed P2Publish for Service ID: {ServiceId}, SubscriptionId: {SubscriptionId}, Payment ID: {PaymentId}",
                                    payment.AdId.Value, subscriptionId, payment.PaymentId);
                                break;
                            }

                        default:

                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing services addons for Payment ID: {PaymentId}", payment.PaymentId);
            }
        }

        private async Task<Dictionary<ProductType, Guid>> GetAddonIdsByProductTypeAsync(PaymentEntity payment, CancellationToken cancellationToken)
        {
            var addonMappings = new Dictionary<ProductType, Guid>();

            if (payment.UserAddonIds == null || !payment.UserAddonIds.Any())
            {
                _logger.LogWarning("No addon IDs found for Payment ID: {PaymentId}", payment.PaymentId);
                return addonMappings;
            }

            try
            {
                var addons = await _subscriptionContext.UserAddOns
                    .Where(ua => payment.UserAddonIds.Contains(ua.UserAddOnId) && ua.PaymentId == payment.PaymentId)
                    .Select(ua => new { ua.UserAddOnId, ua.ProductType })
                    .ToListAsync(cancellationToken);

                if (!addons.Any())
                {
                    _logger.LogWarning("No addon records found in database for Payment ID: {PaymentId}", payment.PaymentId);
                    return addonMappings;
                }

                foreach (var addon in addons)
                {
                    if (addon.ProductType.HasValue)
                    {
                        addonMappings[addon.ProductType.Value] = addon.UserAddOnId;
                        _logger.LogDebug("Mapped AddonId {AddonId} to ProductType {ProductType} for Payment ID: {PaymentId}",
                            addon.UserAddOnId, addon.ProductType.Value, payment.PaymentId);
                    }
                    else
                    {
                        _logger.LogWarning("ProductType is null for AddonId {AddonId} in Payment ID: {PaymentId}",
                            addon.UserAddOnId, payment.PaymentId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapping addon IDs to product types for Payment ID: {PaymentId}", payment.PaymentId);
            }

            return addonMappings;
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
                    var classifiedBaseUrls = _configuration.GetSection("BaseUrl:Classifieds").GetChildren()
                                                          .ToDictionary(x => x.Key, x => x.Value);
                    if (classifiedBaseUrls.TryGetValue(subVertical.ToString(), out var subVerticalUrl))
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
                    baseRedirectUrl = _configuration.GetSection("BaseUrl:Services").Value ?? verticalBaseUrl;
                }
            }
            else
            {
                baseRedirectUrl = _configuration.GetSection("BaseUrl")["LegacyDrupal"] ?? "https://default-legacy-url.com";
            }

            return baseRedirectUrl;
        }
        private async Task<Guid?> ResolveSubscriptionIdForAddonsAsync(
    Vertical vertical,
    SubVertical? subVertical,
    string userId,
    long? adId,
    CancellationToken ct)
        {
            if (adId.HasValue && adId != null && adId > 0)
            {
                var subByAd = await _subscriptionContext.Subscriptions
                    .AsNoTracking()
                    .Where(s =>
                        s.UserId == userId &&
                        s.Vertical == vertical &&
                        (!subVertical.HasValue || s.SubVertical == subVertical.Value) &&
                        s.Status == SubscriptionStatus.Active &&
                        s.EndDate > DateTime.UtcNow &&
                        s.AdId == adId) 
                    .OrderByDescending(s => s.EndDate)
                    .FirstOrDefaultAsync(ct);

                if (subByAd != null)
                    return subByAd.SubscriptionId;
            }
            var freeSub = await _subscriptionContext.Subscriptions
                .AsNoTracking()
                .Where(s =>
                    s.UserId == userId &&
                    s.Vertical == vertical &&
                    (!subVertical.HasValue || s.SubVertical == subVertical.Value) &&
                    s.Status == SubscriptionStatus.Active &&
                    s.EndDate > DateTime.UtcNow &&
                    (s.ProductType == ProductType.FREE))
                .OrderByDescending(s => s.EndDate)
                .FirstOrDefaultAsync(ct);

            if (freeSub != null)
                return freeSub.SubscriptionId;

            var anyActive = await _subscriptionContext.Subscriptions
                .AsNoTracking()
                .Where(s =>
                    s.UserId == userId &&
                    s.Vertical == vertical &&
                    (!subVertical.HasValue || s.SubVertical == subVertical.Value) &&
                    s.Status == SubscriptionStatus.Active &&
                    s.EndDate > DateTime.UtcNow)
                .OrderByDescending(s => s.EndDate)
                .FirstOrDefaultAsync(ct);

            return anyActive?.SubscriptionId;
        }

    }
}
