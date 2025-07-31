using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s.Payments;
using QLN.Common.DTO_s.Payments.QLN.Common.DTO_s.Payments;
using QLN.Common.Infrastructure.IService.IPayments;
using QLN.Common.Infrastructure.QLDbContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.Service.Payments
{
    public class PaymentService : IPaymentService
    {
        private readonly ILogger<PaymentService> _logger;
        private readonly IFatoraService _fatoraService;
        private readonly ID365Service _d365Service;
        private readonly QLPaymentsContext _dbContext;

        public PaymentService(
            ILogger<PaymentService> logger,
            IFatoraService fatoraService,
            ID365Service d365Service,
            QLPaymentsContext dbContext
            )
        {
            _logger = logger;
            _fatoraService = fatoraService;
            _d365Service = d365Service;
            _dbContext = dbContext;
        }

        public Task<PaymentResponse> PayAsync(ExternalPaymentRequest request, CancellationToken cancellationToken = default)
        {
            // I am not going to implement this method as our system differs and this would provide minimal value
            // we trigger payments internal to our backend

            _logger.LogWarning("PayAsync method is not implemented in PaymentService. This method is not used in our system as we handle payments internally.");

            throw new NotImplementedException();
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

            string baseRedirectUrl = "https://yourwebsite.com/payment/failure"; // Replace with your actual base URL

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

            string baseRedirectUrl = "https://yourwebsite.com/payment/failure"; // Replace with your actual base URL

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

            if (!int.TryParse(request.OrderId, out var orderId)) return $"{baseRedirectUrl}?paymentSuccess=false";

            _logger.LogDebug("Processing payment success for Order ID: {OrderId}", orderId);

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

            await _d365Service.SendPaymentInfoD365Async(d365Data, cancellationToken);

            _logger.LogDebug("Payment information sent to D365 for Order ID: {OrderId}", orderId);

            // Return the redirect URL as a string
            return $"{baseRedirectUrl}?paymentSuccess=${paymentConfirmation.Status}&productType=${payment.ProductType}"; // this is just an example, you might want to return a more meaningful URL 
        }

    }
}
