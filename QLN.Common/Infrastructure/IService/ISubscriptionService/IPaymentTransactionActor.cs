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
        /// <summary>
        /// Sets payment transaction data and publishes payment completed message
        /// </summary>
        /// <param name="data">Payment transaction data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful</returns>
        Task<bool> SetDataAsync(PaymentTransactionDto data, CancellationToken cancellationToken = default);

        /// <summary>
        /// Fast version of SetDataAsync with same functionality
        /// </summary>
        /// <param name="data">Payment transaction data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful</returns>
        Task<bool> FastSetDataAsync(PaymentTransactionDto data, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves stored payment transaction data
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Payment transaction data or null if not found</returns>
        Task<PaymentTransactionDto?> GetDataAsync(CancellationToken cancellationToken = default);
    }

}
