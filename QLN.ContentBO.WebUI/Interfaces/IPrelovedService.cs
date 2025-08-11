using QLN.ContentBO.WebUI.Models;

namespace QLN.ContentBO.WebUI.Interfaces
{
    public interface IPrelovedService
    {
        Task<HttpResponseMessage?> GetPrelovedSubscription(PrelovedSubscriptionQuery prelovedSubscriptionQuery);
        Task<HttpResponseMessage?> GetPrelovedP2PTransaction(PrelovedP2PTransactionQuery p2pTransactionQuery);
        Task<HttpResponseMessage?> GetPrelovedUserListing(FilterRequest request);
        Task<HttpResponseMessage?> GetPrelovedP2pListing(PrelovedP2PSubscriptionQuery p2pSubscriptionQuery);
        Task<HttpResponseMessage?> PerformPrelovedBulkActionAsync(object payload);
    }
}
