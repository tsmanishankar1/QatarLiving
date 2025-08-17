using Dapr.Actors;
using Dapr.Actors.Client;
using QLN.Common.DTOs;
using QLN.Common.Infrastructure.Subscriptions;
using System.Collections.Concurrent;

namespace QLN.DataMigration.Services
{
    public class SubscriptionService : IExternalSubscriptionService
    {
        private static readonly ConcurrentDictionary<Guid, byte> _subscriptionIds = new();

        private ISubscriptionActor GetActorProxy(Guid id)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Actor ID cannot be empty", nameof(id));

            return ActorProxy.Create<ISubscriptionActor>(
                new ActorId(id.ToString()),
                "SubscriptionActor");
        }
        public Task<bool> AddUserToRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ChangeUserRoleAsync(Guid userId, string newRole, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<YearlySubscriptionResponseDto?> CheckYearlySubscriptionAsync(string userId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Guid> CreatePaymentAsync(PaymentTransactionRequestDto request, string userId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task CreateSubscriptionAsync(SubscriptionRequestDto request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var id = Guid.NewGuid();

            var dto = new SubscriptionDto
            {
                Id = id,
                subscriptionName = request.SubscriptionName,
                Duration = request.Duration,
                price = request.Price,
                description = request.Description,
                currency = request.Currency,
                adsbudget = request.adsbudget,
                promotebudget = request.promotebudget,
                refreshbudget = request.refreshbudget,
                featurebudget = request.featurebudget,
                CategoryId = request.CategoryId,
                VerticalTypeId = request.VerticalTypeId,
                StatusId = request.StatusId,
                
                lastUpdated = DateTime.UtcNow
            };

            var actor = GetActorProxy(dto.Id);
            var result = await actor.FastSetDataAsync(dto, cancellationToken);

            if (result)
            {
                _subscriptionIds.TryAdd(dto.Id, 0);
                return;
            }

            throw new Exception("Subscription creation failed.");
        }

        public Task<bool> DeleteSubscriptionAsync(Guid id, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<List<SubscriptionResponseDto>> GetAllSubscriptionsAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<SubscriptionGroupResponseDto> GetSubscriptionsByVerticalAndCategoryAsync(int verticalTypeId, int? categoryId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<List<UserPaymentDetailsResponseDto>> GetUserPaymentDetailsAsync(string userId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<string[]> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task HandleSubscriptionExpiryAsync(SubscriptionExpiryMessage message, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> IsUserInRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> RemoveUserFromRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateSubscriptionAsync(Guid subscriptionId, SubscriptionRequestDto request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
