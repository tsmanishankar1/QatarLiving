using QLN.Common.DTOs;
using System;
using System.Threading;
using System.Threading.Tasks;

public interface IExternalSubscriptionService
{
    Task<SubscriptionGroupResponseDto> GetSubscriptionsByVerticalAndCategoryAsync(  int verticalTypeId,  int categoryId,CancellationToken cancellationToken = default);
    Task<List<SubscriptionResponseDto>> GetAllSubscriptionsAsync(CancellationToken cancellationToken = default);
    Task CreateSubscriptionAsync(SubscriptionRequestDto request, CancellationToken cancellationToken = default);

    Task<bool> UpdateSubscriptionAsync(Guid subscriptionId, SubscriptionRequestDto request, CancellationToken cancellationToken);

      Task<bool> DeleteSubscriptionAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> CreatePaymentAsync(PaymentTransactionRequestDto request, Guid userId, CancellationToken cancellationToken = default);
    Task<string[]> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> AddUserToRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default);
    Task<bool> RemoveUserFromRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default);
    Task<bool> IsUserInRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default);
    Task<bool> ChangeUserRoleAsync(Guid userId, string newRole, CancellationToken cancellationToken = default);
    Task HandleSubscriptionExpiryAsync(SubscriptionExpiryMessage message, CancellationToken cancellationToken = default);
}


