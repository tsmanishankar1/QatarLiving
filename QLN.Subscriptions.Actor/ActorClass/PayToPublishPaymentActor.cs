using Dapr.Actors;
using Dapr.Actors.Runtime;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.IPayToPublishService;

namespace QLN.Subscriptions.Actor.ActorClass
{
    public class PayToPublishPaymentActor : Dapr.Actors.Runtime.Actor,IPaymentActor
    {
        private const string StateKey = "paytopublish-payment-data";
        private readonly ILogger<PayToPublishPaymentActor> _logger;

        public PayToPublishPaymentActor(ActorHost host, ILogger<PayToPublishPaymentActor> logger) : base(host)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> SetDataAsync(PaymentDto data, CancellationToken cancellationToken = default)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            _logger.LogInformation("[PaymentActor {ActorId}] SetDataAsync called", Id);

            await StateManager.SetStateAsync(StateKey, data, cancellationToken);
            await StateManager.SaveStateAsync(cancellationToken);

            return true;
        }

        public async Task<bool> FastSetDataAsync(PaymentDto data, CancellationToken cancellationToken = default)
        {
           
            return await SetDataAsync(data, cancellationToken);
        }

        public async Task<PaymentDto?> GetDataAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("[PaymentActor {ActorId}] GetDataAsync called", Id);

            var conditionalValue = await StateManager.TryGetStateAsync<PaymentDto>(StateKey, cancellationToken);

            if (conditionalValue.HasValue)
            {
                return conditionalValue.Value;
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