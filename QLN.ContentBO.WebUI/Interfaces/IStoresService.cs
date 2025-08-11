using QLN.ContentBO.WebUI.Models;

namespace QLN.ContentBO.WebUI.Interfaces
{
    public interface IStoresService
    {
        Task<HttpResponseMessage> GetAllStoresSubscription(StoreSubscriptionQuery query);
        Task<HttpResponseMessage> GetAllStoresListing(CompanyRequestPayload request);
        Task<HttpResponseMessage> GetStoresById(string vertical, string adId);
        Task<HttpResponseMessage> UpdateStoresSubscription(int orderId,string status);
    }
}
