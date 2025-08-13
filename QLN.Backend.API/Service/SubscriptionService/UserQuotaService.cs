using Dapr.Actors.Client;
using Dapr.Actors;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.ISubscriptionService;

namespace QLN.Backend.API.Service.SubscriptionService
{
    public class UserQuotaService : IUserQuotaService
    {
        private readonly ILogger<UserQuotaService> _logger;

        public UserQuotaService(ILogger<UserQuotaService> logger)
        {
            _logger = logger;
        }

        private IUserQuotaActor GetUserQuotaActorProxy(string userId)
        {
            return ActorProxy.Create<IUserQuotaActor>(new ActorId(userId), "UserQuotaActor");
        }

        // Write operation
        public async Task UpsertQuotaAsync(string userId, GenericUserQuotaDto newQuota, CancellationToken cancellationToken = default)
        {
            try
            {
                var actor = GetUserQuotaActorProxy(userId);
                await actor.UpsertQuotaAsync(newQuota, cancellationToken);
                _logger.LogInformation("Upserted quota for user {UserId} ({Source})", userId, newQuota.SourceType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting quota for user {UserId}", userId);
                throw;
            }
        }

        // Read operations
        public async Task<UserQuotaCollection?> GetUserQuotasAsync(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var actor = GetUserQuotaActorProxy(userId);
                var result = await actor.GetAllQuotasAsync(cancellationToken);
                _logger.LogInformation("Retrieved quota collection for user {UserId}: {QuotaCount} quotas found",
                    userId, result?.Quotas?.Count ?? 0);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving quotas for user {UserId}", userId);
                throw;
            }
        }
            
        public async Task<List<GenericUserQuotaDto>> GetActiveUserQuotasAsync(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var actor = GetUserQuotaActorProxy(userId);
                var result = await actor.GetActiveQuotasAsync(cancellationToken);
                _logger.LogInformation("Retrieved {Count} active quotas for user {UserId}", result.Count, userId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active quotas for user {UserId}", userId);
                throw;
            }
        }

        // Update/Delete operations
        public async Task<bool> UpdateUserQuotaAsync(string userId, Guid transactionId, GenericUserQuotaDto updatedQuota, CancellationToken cancellationToken = default)
        {
            try
            {
                var actor = GetUserQuotaActorProxy(userId);
                var result = await actor.UpdateQuotaAsync(transactionId, updatedQuota, cancellationToken);
                _logger.LogInformation("Updated quota for user {UserId}, transaction {TransactionId}: {Success}",
                    userId, transactionId, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating quota for user {UserId}, transaction {TransactionId}", userId, transactionId);
                throw;
            }
        }

        public async Task<bool> DeleteUserQuotaAsync(string userId, Guid transactionId, CancellationToken cancellationToken = default)
        {
            try
            {
                var actor = GetUserQuotaActorProxy(userId);
                var result = await actor.DeleteQuotaAsync(transactionId, cancellationToken);
                _logger.LogInformation("Deleted quota for user {UserId}, transaction {TransactionId}: {Success}",
                    userId, transactionId, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting quota for user {UserId}, transaction {TransactionId}", userId, transactionId);
                throw;
            }
        }
    }
}
