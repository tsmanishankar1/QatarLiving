using Dapr.Actors.Runtime;
using global::QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.IPayToFeatureActor;

namespace QLN.Subscriptions.Actor.ActorClass
{
    public class PayToFeatureActor : Dapr.Actors.Runtime.Actor, IPayToFeatureActor
    {
        private const string StateKey = "pay-to-publish-data";
        private const string BasicPriceStateName = "basicPriceData";
        private const string PlanIdsStateKey = "plan-ids-collection";
        private const string BasicPriceIdsStateKey = "basicprice-ids-collection";
        private const string PaymentIdsStateKey = "payment-ids-collection";
        private const string PayToFeatureDataStateKey = "paytofeature-data";
        private readonly ILogger<PayToFeatureActor> _logger;

        public PayToFeatureActor(ActorHost host, ILogger<PayToFeatureActor> logger) : base(host)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> SetDataAsync(PayToFeatureDto data, CancellationToken cancellationToken = default)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            _logger.LogInformation("[PayToFeatureActor {ActorId}] SetDataAsync called", Id);

            await StateManager.SetStateAsync(StateKey, data, cancellationToken);
            await StateManager.SaveStateAsync(cancellationToken);

            return true;
        }

        public async Task<bool> FastSetDataAsync(PayToFeatureDto data, CancellationToken cancellationToken = default)
        {
            return await SetDataAsync(data, cancellationToken);
        }

        public async Task<PayToFeatureDto?> GetDataAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("[PayToFeatureActor {ActorId}] GetDataAsync called", Id);

            var result = await StateManager.TryGetStateAsync<PayToFeatureDto>(StateKey, cancellationToken);

            return result.HasValue ? result.Value : null;
        }

        public async Task<bool> SetPayToFeatureDataAsync(PayToFeatureDataDto data, CancellationToken cancellationToken = default)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            _logger.LogInformation("[PayToFeatureActor {ActorId}] SetPayToFeatureDataAsync called", Id);

            await StateManager.SetStateAsync(PayToFeatureDataStateKey, data, cancellationToken);
            await StateManager.SaveStateAsync(cancellationToken);

            return true;
        }

        public async Task<PayToFeatureDataDto?> GetPayToFeatureDataAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("[PayToFeatureActor {ActorId}] GetPayToFeatureDataAsync called", Id);

            var result = await StateManager.TryGetStateAsync<PayToFeatureDataDto>(PayToFeatureDataStateKey, cancellationToken);

            return result.HasValue ? result.Value : null;
        }

        public async Task<bool> SetDatasAsync(PayToFeatureBasicPriceDto data, CancellationToken cancellationToken = default)
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

        public async Task<PayToFeatureBasicPriceDto?> GetDatasAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Getting BasicPrice data for actor {ActorId}", Id);

                var conditionalResult = await StateManager.TryGetStateAsync<PayToFeatureBasicPriceDto>(
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
        protected override Task OnActivateAsync()
        {
            _logger.LogInformation("[PayToFeatureActor {ActorId}] Activated", Id);
            return base.OnActivateAsync();
        }

        protected override Task OnDeactivateAsync()
        {
            _logger.LogInformation("[PayToFeatureActor {ActorId}] Deactivated", Id);
            return base.OnDeactivateAsync();
        }
    }
}