using QLN.Common.DTO_s;
using QLN.Common.DTOs;
using System;
using System.Threading;
using System.Threading.Tasks;
[Obsolete("This interface is deprecated and will be removed in future versions. Use the new IV2SubscriptionService interfaces instead.", false)]
public interface IExternalSubscriptionService
{
    Task<SubscriptionGroupResponseDto> GetSubscriptionsByVerticalAndCategoryAsync(  int verticalTypeId,  int? categoryId,CancellationToken cancellationToken = default);
    Task<List<SubscriptionResponseDto>> GetAllSubscriptionsAsync(CancellationToken cancellationToken = default);
    Task CreateSubscriptionAsync(SubscriptionRequestDto request, CancellationToken cancellationToken = default);

    Task<bool> UpdateSubscriptionAsync(Guid subscriptionId, SubscriptionRequestDto request, CancellationToken cancellationToken);

      Task<bool> DeleteSubscriptionAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> CreatePaymentAsync(PaymentTransactionRequestDto request, string userId, CancellationToken cancellationToken = default);
    Task<string[]> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> AddUserToRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default);
    Task<bool> RemoveUserFromRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default);
    Task<bool> IsUserInRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default);
    Task<bool> ChangeUserRoleAsync(Guid userId, string newRole, CancellationToken cancellationToken = default);
    Task HandleSubscriptionExpiryAsync(SubscriptionExpiryMessage message, CancellationToken cancellationToken = default);
    Task<List<UserPaymentDetailsResponseDto>> GetUserPaymentDetailsAsync(   string userId,   CancellationToken cancellationToken = default);
    Task<YearlySubscriptionResponseDto?> CheckYearlySubscriptionAsync(  string userId, CancellationToken cancellationToken = default);
}


