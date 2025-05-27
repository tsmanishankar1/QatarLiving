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



}
