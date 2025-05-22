using Dapr.Actors;
using Dapr.Actors.Runtime;
using Microsoft.Extensions.Logging;
using QLN.Common.DTOs;
using QLN.Common.Infrastructure.IService.ISubscriptionService;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace QLN.Subscriptions
{
    public class PaymentTransactionActor : Actor, IPaymentTransactionActor
    {
        private const string StateKey = "payment-transaction-data";
        private readonly ILogger<PaymentTransactionActor> _logger;

        public PaymentTransactionActor(ActorHost host, ILogger<PaymentTransactionActor> logger) : base(host)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> SetDataAsync(PaymentTransactionDto data, CancellationToken cancellationToken = default)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            _logger.LogInformation("[PaymentActor {ActorId}] SetDataAsync called", Id);
            await StateManager.SetStateAsync(StateKey, data, cancellationToken);
            return true;
        }

        public async Task<bool> FastSetDataAsync(PaymentTransactionDto data, CancellationToken cancellationToken = default)
        {
            // In this simple version, FastSet behaves the same as Set
            return await SetDataAsync(data, cancellationToken);
        }

        public async Task<PaymentTransactionDto?> GetDataAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("[PaymentActor {ActorId}] GetDataAsync called", Id);

            if (await StateManager.ContainsStateAsync(StateKey, cancellationToken))
            {
                return await StateManager.GetStateAsync<PaymentTransactionDto>(StateKey, cancellationToken);
            }

            return null;
        }

        protected override Task OnActivateAsync()
        {
            _logger.LogInformation("[PaymentActor {ActorId}] Actor activated", Id);
            return base.OnActivateAsync();
        }

        protected override Task OnDeactivateAsync()
        {
            _logger.LogInformation("[PaymentActor {ActorId}] Actor deactivated", Id);
            return base.OnDeactivateAsync();
        }
    }
}
