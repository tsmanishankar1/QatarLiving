using Dapr.Actors.Runtime;
using global::QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.IPayToPublicActor;

namespace QLN.Subscriptions.Actor.ActorClass
{
    public class PayToPublishActor : Dapr.Actors.Runtime.Actor, IPayToPublishActor
    {
        private const string StateKey = "pay-to-publish-data";
        private const string BasicPriceStateName = "basicPriceData";
        private const string PlanIdsStateKey = "plan-ids-collection";
        private const string BasicPriceIdsStateKey = "basicprice-ids-collection";
        private const string PaymentIdsStateKey = "payment-ids-collection";
        private const string PayToPublishDataStateKey = "paytopublish-data";
        private const string PaymentDataStateKey = "payment-data";
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

        public async Task<bool> SetPayToPublishDataAsync(PayToPublishDataDto data, CancellationToken cancellationToken = default)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            _logger.LogInformation("[PayToPublishActor {ActorId}] SetPayToPublishDataAsync called", Id);

            await StateManager.SetStateAsync(PayToPublishDataStateKey, data, cancellationToken);
            await StateManager.SaveStateAsync(cancellationToken);

            return true;
        }

        public async Task<PayToPublishDataDto?> GetPayToPublishDataAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("[PayToPublishActor {ActorId}] GetPayToPublishDataAsync called", Id);

            var result = await StateManager.TryGetStateAsync<PayToPublishDataDto>(PayToPublishDataStateKey, cancellationToken);

            return result.HasValue ? result.Value : null;
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

        // Plan ID Management Methods
        public async Task<bool> AddPlanIdAsync(Guid planId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Adding plan ID {PlanId} to actor {ActorId}", planId, Id);

                var planIds = await GetPlanIdsAsync(cancellationToken);
                if (!planIds.Contains(planId))
                {
                    planIds.Add(planId);
                    await StateManager.SetStateAsync(PlanIdsStateKey, planIds, cancellationToken);
                    await StateManager.SaveStateAsync(cancellationToken);

                    _logger.LogInformation("Successfully added plan ID {PlanId} to actor {ActorId}", planId, Id);
                }
                else
                {
                    _logger.LogDebug("Plan ID {PlanId} already exists in actor {ActorId}", planId, Id);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding plan ID {PlanId} to actor {ActorId}", planId, Id);
                return false;
            }
        }

        public async Task<List<Guid>> GetAllPlansAsync(CancellationToken cancellationToken = default)
        {
            return await GetPlanIdsAsync(cancellationToken);
        }

        private async Task<List<Guid>> GetPlanIdsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await StateManager.TryGetStateAsync<List<Guid>>(PlanIdsStateKey, cancellationToken);
                return result.HasValue ? result.Value : new List<Guid>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving plan IDs for actor {ActorId}", Id);
                return new List<Guid>();
            }
        }

        // Basic Price ID Management Methods
        public async Task<bool> AddBasicPriceIdAsync(Guid basicPriceId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Adding basic price ID {BasicPriceId} to actor {ActorId}", basicPriceId, Id);

                var basicPriceIds = await GetBasicPriceIdsAsync(cancellationToken);
                if (!basicPriceIds.Contains(basicPriceId))
                {
                    basicPriceIds.Add(basicPriceId);
                    await StateManager.SetStateAsync(BasicPriceIdsStateKey, basicPriceIds, cancellationToken);
                    await StateManager.SaveStateAsync(cancellationToken);

                    _logger.LogInformation("Successfully added basic price ID {BasicPriceId} to actor {ActorId}", basicPriceId, Id);
                }
                else
                {
                    _logger.LogDebug("Basic price ID {BasicPriceId} already exists in actor {ActorId}", basicPriceId, Id);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding basic price ID {BasicPriceId} to actor {ActorId}", basicPriceId, Id);
                return false;
            }
        }

        public async Task<List<Guid>> GetAllBasicPriceIdsAsync(CancellationToken cancellationToken = default)
        {
            return await GetBasicPriceIdsAsync(cancellationToken);
        }

        private async Task<List<Guid>> GetBasicPriceIdsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await StateManager.TryGetStateAsync<List<Guid>>(BasicPriceIdsStateKey, cancellationToken);
                return result.HasValue ? result.Value : new List<Guid>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving basic price IDs for actor {ActorId}", Id);
                return new List<Guid>();
            }
        }

        // Payment ID Management Methods
        public async Task<bool> AddPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Adding payment ID {PaymentId} to actor {ActorId}", paymentId, Id);

                var paymentIds = await GetPaymentIdsAsync(cancellationToken);
                if (!paymentIds.Contains(paymentId))
                {
                    paymentIds.Add(paymentId);
                    await StateManager.SetStateAsync(PaymentIdsStateKey, paymentIds, cancellationToken);
                    await StateManager.SaveStateAsync(cancellationToken);

                    _logger.LogInformation("Successfully added payment ID {PaymentId} to actor {ActorId}", paymentId, Id);
                }
                else
                {
                    _logger.LogDebug("Payment ID {PaymentId} already exists in actor {ActorId}", paymentId, Id);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding payment ID {PaymentId} to actor {ActorId}", paymentId, Id);
                return false;
            }
        }

        public async Task<List<Guid>> GetAllPaymentIdsAsync(CancellationToken cancellationToken = default)
        {
            return await GetPaymentIdsAsync(cancellationToken);
        }

        private async Task<List<Guid>> GetPaymentIdsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await StateManager.TryGetStateAsync<List<Guid>>(PaymentIdsStateKey, cancellationToken);
                return result.HasValue ? result.Value : new List<Guid>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving payment IDs for actor {ActorId}", Id);
                return new List<Guid>();
            }
        }

        // Actor lifecycle methods
        protected override Task OnActivateAsync()
        {
            _logger.LogInformation("[PayToPublishActor {ActorId}] Activated", Id);
            return base.OnActivateAsync();
        }

        protected override Task OnDeactivateAsync()
        {
            _logger.LogInformation("[PayToPublishActor {ActorId}] Deactivated", Id);
            return base.OnDeactivateAsync();
        }
    }
}