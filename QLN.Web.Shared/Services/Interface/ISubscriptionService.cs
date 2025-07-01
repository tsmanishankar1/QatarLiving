using QLN.Web.Shared.Models;


namespace QLN.Web.Shared.Services.Interface
{
    public interface ISubscriptionService
    {
        Task<bool> AddSubscriptionAsync(SubscriptionModel model);
        Task<SubscriptionResponse?> GetSubscriptionAsync(int verticalTypeId,int categoryId);
        Task<bool> UpdateSubscriptionAsync(SubscriptionModel model);
        Task<bool> DeleteSubscriptionAsync(Guid id);
        Task<bool> PurchaseSubscription(object model);
        Task<List<PayToPublishPlan?>> GetPayToPublishPlansAsync(int verticalTypeId, int categoryId);        
        Task<List<PayToPublishPlan?>> GetPayToFeatureAsync(int verticalTypeId, int categoryId);


    }
}
