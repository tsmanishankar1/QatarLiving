using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s.Payments;
using QLN.Common.Infrastructure.IService.IPayments;
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

        public PaymentService(
            ILogger<PaymentService> logger,
            IFatoraService fatoraService
            )
        {
            _logger = logger;
            _fatoraService = fatoraService;
        }

        public Task<PaymentResponse> PayAsync(ExternalPaymentRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<string> PaymentFailureAsync(PaymentTransactionRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<string> PaymentSuccessAsync(PaymentTransactionRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
