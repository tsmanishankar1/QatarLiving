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
            /// <returns>True if data was successfully stored</returns>
            Task<bool> SetDataAsync(PaymentTransactionDto data, CancellationToken cancellationToken = default);

            /// <summary>
            /// Fast version of SetData - currently identical to SetDataAsync
            /// </summary>
            /// <param name="data">Payment transaction data to store</param>
            /// <param name="cancellationToken">Cancellation token</param>
            /// <returns>True if data was successfully stored</returns>
            Task<bool> FastSetDataAsync(PaymentTransactionDto data, CancellationToken cancellationToken = default);

            /// <summary>
            /// Retrieves payment transaction data from state store
            /// </summary>
            /// <param name="cancellationToken">Cancellation token</param>
            /// <returns>Payment transaction data or null if not found</returns>
            Task<PaymentTransactionDto?> GetDataAsync(CancellationToken cancellationToken = default);

            /// <summary>
            /// Deletes payment transaction data and cleans up timers/reminders
            /// </summary>
            /// <param name="cancellationToken">Cancellation token</param>
            /// <returns>True if data was successfully deleted</returns>
            Task<bool> DeleteDataAsync(CancellationToken cancellationToken = default);

            /// <summary>
            /// Synchronizes data between primary and backup state keys
            /// </summary>
            /// <param name="cancellationToken">Cancellation token</param>
            /// <returns>True if synchronization was successful</returns>
            Task<bool> SyncStateKeysAsync(CancellationToken cancellationToken = default);

            /// <summary>
            /// Manually triggers subscription expiry check
            /// </summary>
            /// <returns>Task representing the asynchronous operation</returns>
            Task CheckSubscriptionExpiryAsync();
        }
    }


