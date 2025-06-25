using Dapr.Actors.Runtime;
using global::QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.IPayToPublicActor;


namespace QLN.Subscriptions.Actor.ActorClass
{
    public class PayToPublishActor : Dapr.Actors.Runtime.Actor, IPayToPublishActor
    {
        private const string StateKey = "pay-to-publish-data";
        private const string BasicPriceStateName = "basicPriceData";
        private readonly ILogger<PayToPublishActor> _logger;

        public PayToPublishActor(ActorHost host, ILogger<PayToPublishActor> logger) : base(host)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> SetDataAsync(PayToPublishDto data, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(data);

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
        public async Task<bool> SetDatasAsync(BasicPriceDto data, CancellationToken cancellationToken = default)
        {
            try
            {
                if (data == null)
                {
                    _logger.LogWarning("Attempted to set null BasicPriceDto for actor {ActorId}", Id);
                    return false;
                }

                _logger.LogInformation("Setting BasicPrice data for actor {ActorId}", Id);

                await StateManager.SetStateAsync(BasicPriceStateName, data, cancellationToken);
                await StateManager.SaveStateAsync(cancellationToken);

                _logger.LogInformation("Successfully set BasicPrice data for actor {ActorId}", Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting BasicPrice data for actor {ActorId}", Id);
                return false;
            }
        }

        public async Task<BasicPriceDto?> GetDatasAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Getting BasicPrice data for actor {ActorId}", Id);

                var conditionalResult = await StateManager.TryGetStateAsync<BasicPriceDto>(
                    BasicPriceStateName,
                    cancellationToken);

                if (conditionalResult.HasValue)
                {
                    _logger.LogDebug("Successfully retrieved BasicPrice data for actor {ActorId}", Id);
                    return conditionalResult.Value;
                }

                _logger.LogDebug("No BasicPrice data found for actor {ActorId}", Id);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting BasicPrice data for actor {ActorId}", Id);
                return null;
            }
        }

    }

}