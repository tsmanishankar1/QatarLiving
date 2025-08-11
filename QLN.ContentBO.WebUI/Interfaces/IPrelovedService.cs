using QLN.ContentBO.WebUI.Models;

namespace QLN.ContentBO.WebUI.Interfaces
{
    public interface IPrelovedService
    {
        Task<HttpResponseMessage?> GetPrelovedSubscription(PrelovedSubscriptionQuery request);
        Task<HttpResponseMessage?> GetPrelovedP2pTransaction(FilterRequest request);
        Task<HttpResponseMessage?> GetPrelovedUserListing(FilterRequest request);
        Task<HttpResponseMessage?> GetPrelovedP2pListing(FilterRequest request);
        Task<HttpResponseMessage?> PerformPrelovedBulkActionAsync(object payload);
    }
}
