using Dapr.Actors;
using QLN.Common.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.ISubscriptionService
{
    public interface IPaymentTransactionActor : IActor
    {
        Task<bool> FastSetDataAsync(PaymentTransactionDto data, CancellationToken cancellationToken = default);
        Task<bool> SetDataAsync(PaymentTransactionDto data, CancellationToken cancellationToken = default);
        Task<PaymentTransactionDto?> GetDataAsync(CancellationToken cancellationToken = default);
    }

}
