using Dapr.Actors;
using QLN.Common.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.ISubscriptionService
{
    // Required interface definition

    /// <summary>
    /// Interface for Payment Transaction Actor that manages subscription payment data and expiry checks
    /// </summary>
      public interface IPaymentTransactionActor : IActor
    {
        /// <summary>
        /// Sets payment transaction data and schedules expiry checks
        /// </summary>
        /// <param name="data">Payment transaction data to store</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> SetDataAsync(PaymentTransactionDto data, CancellationToken cancellationToken = default);

        /// <summary>
        /// Fast method to set payment transaction data (alias for SetDataAsync)
        /// </summary>
        /// <param name="data">Payment transaction data to store</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> FastSetDataAsync(PaymentTransactionDto data, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves payment transaction data
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Payment transaction data or null if not found</returns>
        Task<PaymentTransactionDto?> GetDataAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stores payment details for quick retrieval and historical purposes
        /// </summary>
        /// <param name="paymentDetails">Payment details to store</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> StorePaymentDetailsAsync(UserPaymentDetailsResponseDto paymentDetails, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves stored payment details
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Payment details or null if not found</returns>
        Task<UserPaymentDetailsResponseDto?> GetPaymentDetailsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes all data (transaction data and payment details) and cleans up timers
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> DeleteDataAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes only transaction data but keeps payment details for historical purposes
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> RemoveTransactionDataAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Manually triggers subscription expiry check (used by timer)
        /// </summary>
        /// <returns>Task representing the operation</returns>
        Task CheckSubscriptionExpiryAsync();
    }
}


