using Dapr.Actors.Runtime;
using global::QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.IPayToPublicActor;
using QLN.Common.Infrastructure.IService.ISubscriptionService;


namespace QLN.Subscriptions.Actor.ActorClass
{
    public class PayToPublishActor : Dapr.Actors.Runtime.Actor, IPayToPublishActor
    {
        private const string StateKey = "pay-to-publish-data";
        private readonly ILogger<PayToPublishActor> _logger;

        public PayToPublishActor(ActorHost host, ILogger<PayToPublishActor> logger) : base(host)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> SetDataAsync(PayToPublishDto data, CancellationToken cancellationToken = default)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            _logger.LogInformation("[PayToPublishActor {ActorId}] SetDataAsync called", Id);

            await StateManager.SetStateAsync(StateKey, data, cancellationToken);
            await StateManager.SaveStateAsync(cancellationToken);

            return true;
        }

        public async Task<bool> FastSetDataAsync(PayToPublishDto data, CancellationToken cancellationToken = default)
        {
            // Same logic as SetDataAsync for now
            return await SetDataAsync(data, cancellationToken);
        }

        public async Task<PayToPublishDto?> GetDataAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("[PayToPublishActor {ActorId}] GetDataAsync called", Id);

            var result = await StateManager.TryGetStateAsync<PayToPublishDto>(StateKey, cancellationToken);

            return result.HasValue ? result.Value : null;
        }

        protected override Task OnActivateAsync()
        {
            _logger.LogInformation("[PayToPublishActor {ActorId}] Activated", Id);
            return base.OnActivateAsync();
        }
    }

}