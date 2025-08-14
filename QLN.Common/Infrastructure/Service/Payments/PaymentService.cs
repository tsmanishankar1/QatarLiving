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
        private readonly IV2SubscriptionService _subscriptionService;

        public PaymentService(
            ILogger<PaymentService> logger,
            IFatoraService fatoraService,
            ID365Service d365Service,
            QLPaymentsContext dbContext,
            IConfiguration configuration,
            IV2SubscriptionService subscriptionService
            )
        {
            _logger = logger;
            _fatoraService = fatoraService;
            _d365Service = d365Service;
            _dbContext = dbContext;
            _configuration = configuration;
            _subscriptionService = subscriptionService;

        }

        public async Task<PaymentResponse> PayAsync(ExternalPaymentRequest request, CancellationToken cancellationToken = default)
        {
            // Steps followed in this method:
            // 1. Validate the request parameters.
            // 2. Check if the product type is supported.
            // 3. If the product type is supported, create a payment request using the Fatora service.
            // 4. If the product type is not supported, log an error and return a failure response.

            var platform = "web";

            if (string.IsNullOrEmpty(request.User.UserName) || string.IsNullOrEmpty(request.User.UserId))
            {
                _logger.LogError("Username is null or empty");
                // Return a failure response
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
                // Return a failure response
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
                // Return a failure response
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
                    _logger.LogInformation("Processing Order payment for Order ID: {OrderId}", request.OrderId);
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

                   /* // Create the subscription
                    var subscriptionId = await _subscriptionService.PurchaseSubscriptionAsync(subscriptionRequest, cancellationToken);

                    // After subscription is created, update PaymentEntity with subscription info
                    dbResult.Entity.UserSubscriptionId = subscriptionId;

                    _dbContext.SaveChanges(); // Update the payment entity in the database with subscription info*/

                    return await _fatoraService.CreatePaymentAsync(request, request.User.UserName, request.Vertical, request.SubVertical, request.User.Email, request.User.Mobile, platform, cancellationToken);

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
                    };

                   /* // Create the subscription
                    var pubsubscriptionId = await _subscriptionService.PurchaseSubscriptionAsync(publishRequest, cancellationToken);

                    // After subscription is created, update PaymentEntity with subscription info
                    publishDbResult.Entity.UserSubscriptionId = pubsubscriptionId;

                    _dbContext.SaveChanges(); // Update the payment entity in the database with subscription info*/

                    return await _fatoraService.CreatePaymentAsync(request, request.User.UserName, request.Vertical, request.SubVertical, request.User.Email, request.User.Mobile, platform, cancellationToken);

                case ProductType.ADDON_COMBO:
                case ProductType.ADDON_FEATURE:
                case ProductType.ADDON_REFRESH:
                    _logger.LogInformation("Processing Addon payment for Order ID: {OrderId}", request.OrderId);
                    var addonDbResult = _dbContext.Payments.Add(new PaymentEntity
                    {
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

                   /* // Create the addon
                    var addonId = await _subscriptionService.PurchaseAddonAsync(addonRequest, cancellationToken);

                    // After addon is created, update PaymentEntity with addon info
                    addonDbResult.Entity.UserAddonId = addonId;

                    _dbContext.SaveChanges(); // Update the payment entity in the database with addon info*/

                    return await _fatoraService.CreatePaymentAsync(request, request.User.UserName, request.Vertical, request.SubVertical, request.User.Email, request.User.Mobile, platform, cancellationToken);

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

        public async Task<string> PaymentFailureAsync(PaymentTransactionRequest request, CancellationToken cancellationToken = default)
        {
            // Steps followed in this method:
            // 1. Check if the order ID is valid and parse it.
            // 2. Retrieve the payment from the database using the order ID.
            // 3. If the payment is not found, return a failure URL.
            // 4. Update the payment status to Failure and set the transaction ID.
            // 5. Save the changes to the database.
            // 6. Send the payment information to D365 with the operation set to Failure.

            string baseRedirectUrl = GenerateRedirectURLBase(request.Vertical, request.SubVertical);

            if (!int.TryParse(request.OrderId, out var orderId)) return $"{baseRedirectUrl}?paymentSuccess=false";

            _logger.LogDebug("Processing payment failure for Order ID: {OrderId}", orderId);

            var payment = await _dbContext.Payments
                .FirstOrDefaultAsync(p => p.PaymentId == orderId, cancellationToken);

            _logger.LogDebug("Retrieved payment: {Payment}", payment?.PaymentId);

            if (payment == null)
            {
                _logger.LogError("Payment not found for Order ID: {OrderId}", orderId);
                return $"{baseRedirectUrl}?paymentSuccess=false"; // this is just an example, you might want to return a more meaningful URL 
            }

            payment.Status = PaymentStatus.Failure;
            payment.TransactionId = request.TransactionId;

            _dbContext.Update(payment);

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Payment status updated to Failure for Order ID: {OrderId}", orderId);

            var d365Data = new D365Data
            {
                PaymentInfo = payment,
                Operation = D365PaymentOperations.FAILURE
            };

            // Send the payment information to D365
            await _d365Service.SendPaymentInfoD365Async(d365Data, cancellationToken);

            _logger.LogDebug("Payment information sent to D365 for Order ID: {OrderId}", orderId);

            // Return the redirect URL as a string
            return $"{baseRedirectUrl}?paymentSuccess=false"; // this is just an example, you might want to return a more meaningful URL 
        }

        

        public async Task<string> PaymentSuccessAsync(PaymentTransactionRequest request, CancellationToken cancellationToken = default)
        {
            // Steps followed in this method:
            // 1. Verify the payment using the Fatora service.
            // 2. If the payment verification fails, return a failure URL.
            // 3. If the payment verification is successful, update the payment status in the database.
            // 4. Send the payment information to D365.
            // 5. Return a success URL with the payment status and product type.

            string baseRedirectUrl = GenerateRedirectURLBase(request.Vertical, request.SubVertical);

            if (!int.TryParse(request.OrderId, out var orderId)) return $"{baseRedirectUrl}?paymentSuccess=false";

            _logger.LogDebug("Processing payment success for Order ID: {OrderId}", orderId);
            
            var paymentConfirmation = await _fatoraService.VerifyPayment(request.OrderId, cancellationToken);

            if(paymentConfirmation == null)
            {
                _logger.LogError("Payment confirmation is null for Order ID: {OrderId}", request.OrderId);
                return $"{baseRedirectUrl}?paymentSuccess=false"; // this is just an example, you might want to return a more meaningful URL 
            }

            if (paymentConfirmation == null || paymentConfirmation.Status != "SUCCESS")
            {
                _logger.LogError("Payment verification failed for Order ID: {OrderId}", request.OrderId);
                return $"{baseRedirectUrl}?paymentSuccess=false"; // this is just an example, you might want to return a more meaningful URL 
            }

            var payment = await _dbContext.Payments
                .FirstOrDefaultAsync(p => p.PaymentId == orderId || p.AttachedPaymentId == orderId, cancellationToken);

            _logger.LogDebug("Retrieved payment: {Payment}", payment?.PaymentId);

            if (payment == null)
            {
                _logger.LogError("Payment not found for Order ID: {OrderId}", orderId);
                return $"{baseRedirectUrl}?paymentSuccess=false"; // this is just an example, you might want to return a more meaningful URL 
            }

            payment.Status = PaymentStatus.Success;
            payment.TransactionId = paymentConfirmation.Result.TransactionId;
            payment.TriggeredSource = TriggeredSource.Web; // defaulting to Web

            _dbContext.Update(payment);

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Payment status updated to Success for Order ID: {OrderId}", orderId);

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

            // Send payment information to D365
            await _d365Service.SendPaymentInfoD365Async(d365Data, cancellationToken);

            _logger.LogDebug("Payment information sent to D365 for Order ID: {OrderId}", orderId);

            // Return the redirect URL as a string
            return $"{baseRedirectUrl}?paymentSuccess=${paymentConfirmation.Status}&productType=${payment.ProductType}"; // this is just an example, you might want to return a more meaningful URL 
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
                        return subVerticalUrl + "/dashboard";
                    }
                    else
                    {
                        baseRedirectUrl = classifiedBaseUrls.GetValueOrDefault("Default", verticalBaseUrl);
                    }
                }
                else if(vertical == Vertical.Services)
                {
                    var servicesBaseUrls = _configuration.GetSection("BaseUrl:Services").ToString();
                    baseRedirectUrl = servicesBaseUrls;
                }
            }
            else
            {
                baseRedirectUrl = _configuration.GetSection("BaseUrl")["LegacyDrupal"] ?? "https://default-legacy-url.com";
            }

            return baseRedirectUrl + "/dashboard";
        }
    }
}
