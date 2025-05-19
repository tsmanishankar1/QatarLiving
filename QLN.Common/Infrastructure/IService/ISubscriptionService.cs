using QLN.Common.DTO_s;
using System;
using System.Threading;
using System.Threading.Tasks;

public interface ISubscriptionService
{
    Task<SubscriptionDto?> GetSubscriptionAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> CreateSubscriptionAsync(SubscriptionDto subscription, CancellationToken cancellationToken = default);

    Task<bool> UpdateSubscriptionAsync(SubscriptionDto subscription, CancellationToken cancellationToken = default);

    Task<bool> DeleteSubscriptionAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExpireSubscriptionAsync(Guid id, CancellationToken cancellationToken = default);
}
