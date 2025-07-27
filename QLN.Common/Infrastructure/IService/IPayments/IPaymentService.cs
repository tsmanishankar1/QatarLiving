using QLN.Common.DTO_s.Payments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.IPayments
{
    public interface IPaymentService
    {
        // pay
        Task<PaymentResponse> PayAsync(ExternalPaymentRequest request, CancellationToken cancellationToken = default);

        // success

        Task<string> PaymentSuccessAsync(PaymentTransactionRequest request, CancellationToken cancellationToken = default);

        // failure

        Task<string> PaymentFailureAsync(PaymentTransactionRequest request, CancellationToken cancellationToken = default);
    }
}
