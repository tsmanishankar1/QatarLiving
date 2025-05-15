using QLN.Common.DTO_s;
using System;
using System.Threading.Tasks;

public interface ISubscriptionService
{
    Task CreateSubscriptionAsync(SubscriptionDto subscription);
    Task<SubscriptionDto?> GetSubscriptionByIdAsync(Guid id);
    Task ExpireSubscriptionAsync(Guid id);
}
