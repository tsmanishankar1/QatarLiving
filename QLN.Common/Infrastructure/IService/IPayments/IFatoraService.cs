using QLN.Common.DTO_s.Payments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.IPayments
{
    public interface IFatoraService
    {
        public Task<PaymentResponse> CreatePaymentAsync(ExternalPaymentRequest request, string username, string? email, string? mobile, string? platform, CancellationToken cancellationToken = default);
        public Task<FatoraVerificationResponse> VerifyPayment(string orderId, CancellationToken cancellationToken = default);
    }
}
