using QLN.Web.Shared.Models;


namespace QLN.Web.Shared.Services.Interfaces
{
    public interface ISubscriptionService
    {
        Task<bool> AddSubscriptionAsync(SubscriptionModel model);
        Task<SubscriptionModel?> GetSubscriptionAsync(Guid id);
        Task<bool> UpdateSubscriptionAsync(SubscriptionModel model);
        Task<bool> DeleteSubscriptionAsync(Guid id);
    }
}
