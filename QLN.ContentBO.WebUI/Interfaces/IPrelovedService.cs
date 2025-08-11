using QLN.ContentBO.WebUI.Models;

namespace QLN.ContentBO.WebUI.Interfaces
{
    public interface IPrelovedService
    {
        Task<HttpResponseMessage?> GetPrelovedSubscription(PrelovedSubscriptionQuery request);
        Task<HttpResponseMessage?> GetPrelovedP2PTransaction(PrelovedP2PTransactionQuery query);
        Task<HttpResponseMessage?> GetPrelovedUserListing(FilterRequest request);
        Task<HttpResponseMessage?> GetPrelovedP2pListing(FilterRequest request);
        Task<HttpResponseMessage?> PerformPrelovedBulkActionAsync(object payload);
    }
}
