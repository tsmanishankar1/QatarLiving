using Dapr.Actors.Runtime;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.ISubscriptionService;

namespace QLN.Subscriptions.Actor.ActorClass
{
    public class UserQuotaActor : Dapr.Actors.Runtime.Actor, IUserQuotaActor
    {
        private const string StateKey = "data";
        private readonly ILogger<UserQuotaActor> _logger;

        public UserQuotaActor(ActorHost host, ILogger<UserQuotaActor> logger) : base(host)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        public async Task<List<GenericUserQuotaDto>> GetQuotasAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("[UserQuotaActor {ActorId}] GetQuotasAsync called", Id);

                // Use the correct state key and data structure
                var collection = await GetQuotaCollectionAsync(cancellationToken);

                var quotas = collection?.Quotas ?? new List<GenericUserQuotaDto>();

                _logger.LogInformation("[UserQuotaActor {ActorId}] GetQuotasAsync retrieved {Count} quotas",
                    Id, quotas.Count);

                return quotas;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UserQuotaActor {ActorId}] Error in GetQuotasAsync", Id);
                throw;
            }
        }

        public async Task UpsertQuotaAsync(GenericUserQuotaDto newQuota, CancellationToken cancellationToken = default)
        {
            if (newQuota == null) throw new ArgumentNullException(nameof(newQuota));

            try
            {
                _logger.LogInformation("[UserQuotaActor {ActorId}] UpsertQuotaAsync called for transaction {TransactionId}",
                    Id, newQuota.PaymentTransactionId);

                // Get existing collection or create new one (same logic as original UserQuotaWriter)
                var existing = await GetQuotaCollectionAsync(cancellationToken)
                              ?? new UserQuotaCollection { UserId = Id.GetId() };

                // Remove existing quota with same source type and transaction ID (same logic as original UserQuotaWriter)
                existing.Quotas.RemoveAll(q =>
                    q.SourceType == newQuota.SourceType &&
                    q.PaymentTransactionId == newQuota.PaymentTransactionId);

                // Add the new quota
                existing.Quotas.Add(newQuota);

                // Save to actor state
                await SaveQuotaCollectionAsync(existing, cancellationToken);

                _logger.LogInformation("[UserQuotaActor {ActorId}] Upserted quota for user {UserId} ({Source})",
                    Id, Id.GetId(), newQuota.SourceType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UserQuotaActor {ActorId}] Error in UpsertQuotaAsync", Id);
                throw;
            }
        }
        public async Task<UserQuotaCollection?> GetAllQuotasAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("[UserQuotaActor {ActorId}] GetAllQuotasAsync called", Id);
                var collection = await GetQuotaCollectionAsync(cancellationToken);

                _logger.LogInformation("[UserQuotaActor {ActorId}] Retrieved {Count} quotas",
                    Id, collection?.Quotas?.Count ?? 0);

                return collection;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UserQuotaActor {ActorId}] Error in GetAllQuotasAsync", Id);
                throw;
            }
        }

        public async Task<List<GenericUserQuotaDto>> GetActiveQuotasAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("[UserQuotaActor {ActorId}] GetActiveQuotasAsync called", Id);

                var collection = await GetQuotaCollectionAsync(cancellationToken);
                if (collection?.Quotas == null)
                    return new List<GenericUserQuotaDto>();

                var currentDate = DateTime.UtcNow;
                var activeQuotas = collection.Quotas
                    .Where(q => q.EndDate > currentDate && q.StartDate <= currentDate)
                    .ToList();

                _logger.LogInformation("[UserQuotaActor {ActorId}] Found {ActiveCount} active quotas out of {TotalCount}",
                    Id, activeQuotas.Count, collection.Quotas.Count);

                return activeQuotas;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UserQuotaActor {ActorId}] Error in GetActiveQuotasAsync", Id);
                throw;
            }
        }

        // Update operation
        public async Task<bool> UpdateQuotaAsync(Guid transactionId, GenericUserQuotaDto updatedQuota, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("[UserQuotaActor {ActorId}] UpdateQuotaAsync called for transaction {TransactionId}",
                    Id, transactionId);

                var collection = await GetQuotaCollectionAsync(cancellationToken);
                if (collection?.Quotas == null)
                {
                    _logger.LogWarning("[UserQuotaActor {ActorId}] No quota collection found for update", Id);
                    return false;
                }

                var existingQuota = collection.Quotas.FirstOrDefault(q => q.PaymentTransactionId == transactionId);
                if (existingQuota == null)
                {
                    _logger.LogWarning("[UserQuotaActor {ActorId}] No quota found with transaction ID {TransactionId}",
                        Id, transactionId);
                    return false;
                }

                // Update the quota
                var index = collection.Quotas.IndexOf(existingQuota);
                collection.Quotas[index] = updatedQuota;

                await SaveQuotaCollectionAsync(collection, cancellationToken);

                _logger.LogInformation("[UserQuotaActor {ActorId}] Updated quota with transaction ID {TransactionId}",
                    Id, transactionId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UserQuotaActor {ActorId}] Error in UpdateQuotaAsync", Id);
                throw;
            }
        }

        // Delete operation
        public async Task<bool> DeleteQuotaAsync(Guid transactionId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("[UserQuotaActor {ActorId}] DeleteQuotaAsync called for transaction {TransactionId}",
                    Id, transactionId);

                var collection = await GetQuotaCollectionAsync(cancellationToken);
                if (collection?.Quotas == null)
                {
                    _logger.LogWarning("[UserQuotaActor {ActorId}] No quota collection found for deletion", Id);
                    return false;
                }

                var initialCount = collection.Quotas.Count;
                collection.Quotas.RemoveAll(q => q.PaymentTransactionId == transactionId);
                var finalCount = collection.Quotas.Count;

                if (initialCount != finalCount)
                {
                    await SaveQuotaCollectionAsync(collection, cancellationToken);
                    _logger.LogInformation("[UserQuotaActor {ActorId}] Deleted quota with transaction ID {TransactionId}",
                        Id, transactionId);
                    return true;
                }

                _logger.LogWarning("[UserQuotaActor {ActorId}] No quota found to delete with transaction ID {TransactionId}",
                    Id, transactionId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UserQuotaActor {ActorId}] Error in DeleteQuotaAsync", Id);
                throw;
            }
        }

        // Private helper methods
        private async Task<UserQuotaCollection?> GetQuotaCollectionAsync(CancellationToken cancellationToken)
        {
            var conditionalValue = await StateManager.TryGetStateAsync<UserQuotaCollection>(StateKey, cancellationToken);
            return conditionalValue.HasValue ? conditionalValue.Value : null;
        }

        private async Task SaveQuotaCollectionAsync(UserQuotaCollection collection, CancellationToken cancellationToken)
        {
            await StateManager.SetStateAsync(StateKey, collection, cancellationToken);
            await StateManager.SaveStateAsync(cancellationToken);
        }

        protected override Task OnActivateAsync()
        {
            _logger.LogInformation("[UserQuotaActor {ActorId}] Actor activated", Id);
            return base.OnActivateAsync();
        }
    }
}
