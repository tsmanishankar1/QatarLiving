using QLN.Common.DTO_s;
using QLN.Common.DTO_s.Payments;
using QLN.Common.Infrastructure.Subscriptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.IPayments
{
    public interface IFatoraService
    {
        public Task<PaymentResponse> CreatePaymentAsync(ExternalPaymentRequest request, string username, Vertical vertical, SubVertical? subVertical, string? email, string? mobile, string? platform, CancellationToken cancellationToken = default);
        public Task<FatoraVerificationResponse> VerifyPayment(string orderId, CancellationToken cancellationToken = default);
    }
}
