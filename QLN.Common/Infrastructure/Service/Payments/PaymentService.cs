using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s.Payments;
using QLN.Common.DTO_s.Payments.QLN.Common.DTO_s.Payments;
using QLN.Common.DTO_s.Subscription;
using QLN.Common.DTOs;
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


            var username = string.Empty; // You might want to get this from the request context or user claims
            var userId = string.Empty; // You might want to get this from the request context or user claims
            var email = string.Empty; // You might want to get this from the request context or user claims
            var mobile = string.Empty; // You might want to get this from the request context or user claims
            var platform = "web"; // Defaulting to web, you might want to get this from the request context or user claims

            if(string.IsNullOrEmpty(username) || string.IsNullOrEmpty(userId))
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

            if (string.IsNullOrEmpty(email) && string.IsNullOrEmpty(mobile))
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


            // this could be used in conjunction with a switch statement to handle different product codes or
            // in this case product types
            // Below is just an example of how you might handle different product types and an example payment process
            switch (request.ProductType)
            {
                case ProductType.SUBSCRIPTION:
                    _logger.LogInformation("Processing Order payment for Order ID: {OrderId}", request.OrderId);
                    // 
                    var dbResult = _dbContext.Payments.Add(new PaymentEntity
                    {
                        ProductType = request.ProductType ?? ProductType.FREE,
                        UserSubscriptionId = request.UserSubscriptionId,
                        Status = PaymentStatus.Pending,
                        Fee = request.Amount ?? 0,
                        PaidByUid = username,
                        Source = platform == "web" ? Source.Web : Source.Mobile,
                        Gateway = Gateway.FATORA,
                        Vertical = request.Vertical,
                        TriggeredSource = platform == "web" ? TriggeredSource.Web : TriggeredSource.Cron,
                    });

                    _dbContext.SaveChanges();

                    request.OrderId = dbResult.Entity.PaymentId; // Assuming PaymentId is the OrderId

                    _logger.LogInformation("Created PaymentEntity with ID: {PaymentId}", dbResult.Entity.PaymentId);

                    // then create the subscription in an unpaid state

                    // I am assuming these are wrong but just wanted to have an example
                    var subscriptionRequest = new V2SubscriptionPurchaseRequestDto
                    {
                        PaymentId = dbResult.Entity.PaymentId,
                        CompanyId = Guid.TryParse(request.UserSubscriptionId, out var companyId) ? companyId : Guid.NewGuid(),
                        ProductCode = request.ProductType.ToString()
                    };

                    await _subscriptionService.PurchaseSubscriptionAsync(subscriptionRequest, username, cancellationToken);

                    // and then create the payment request using the Fatora service

                    // this is just to give an example of how to create a payment request
                    return await _fatoraService.CreatePaymentAsync(request, username, email, mobile, platform, cancellationToken);

                case ProductType.ADDON_REFRESH:
                    _logger.LogInformation("Processing Addon Refresh payment for Order ID: {OrderId}", request.OrderId);
                    // Handle Addon Refresh payment logic here
                    break;
                case ProductType.ADDON_FEATURE:
                    _logger.LogInformation("Processing Addon Feature payment for Order ID: {OrderId}", request.OrderId);
                    // Handle Addon Feature payment logic here
                    break;
                case ProductType.PUBLISH:
                    _logger.LogInformation("Processing Pay to Publish payment for Order ID: {OrderId}", request.OrderId);
                    // Handle Pay to Publish payment logic here
                    break;
                default:
                    _logger.LogError("Unsupported ProductType: {ProductType}", request.ProductType);
                    break;
            }

            // Return a failure response
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

            string baseRedirectUrl = GenerateRedirectURLBase(request.SubscriptionCategory);

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

            string baseRedirectUrl = GenerateRedirectURLBase(request.SubscriptionCategory);

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
            };

            // Send payment information to D365
            await _d365Service.SendPaymentInfoD365Async(d365Data, cancellationToken);

            _logger.LogDebug("Payment information sent to D365 for Order ID: {OrderId}", orderId);

            // Return the redirect URL as a string
            return $"{baseRedirectUrl}?paymentSuccess=${paymentConfirmation.Status}&productType=${payment.ProductType}"; // this is just an example, you might want to return a more meaningful URL 
        }

        // helper method to know how to gernerate the base redirect URL based on the subscription category - needs IConfiguration import for these various base URLs
        private string GenerateRedirectURLBase(SubscriptionCategory? category)
        {
            string baseRedirectUrl = string.Empty;

            switch (category)
            {
                case SubscriptionCategory.Items:
                case SubscriptionCategory.Deals:
                case SubscriptionCategory.Collectibles:
                case SubscriptionCategory.Preloved:
                case SubscriptionCategory.Stores:
                    baseRedirectUrl = "https://qlc-dev.qatarliving.com";
                    break;
                case SubscriptionCategory.Services:
                    baseRedirectUrl = "https://qls-dev.qatarliving.com";
                    break;
                default:
                    baseRedirectUrl = _configuration.GetSection("BaseUrl")["LegacyDrupal"] ?? throw new ArgumentNullException("LegacyDrupal"); ; // Default URL, you might want to change this
                    break;
            }

            return baseRedirectUrl;
        }
    }
}
