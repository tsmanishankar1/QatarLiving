using QLN.ContentBO.WebUI.Models;

namespace QLN.ContentBO.WebUI.Interfaces
{
    public interface IDealsService
    {
        Task<HttpResponseMessage> GetAllDealsSubscription(DealsSubscriptionQuery dealsSubscriptionQuery);
        Task<HttpResponseMessage> GetAllDealsListing(DealsItemQuery dealsItemQuery);
        Task<HttpResponseMessage?> BulkActionAsync(List<long?> adIds, int action, string? reason = null, string? comments = null);
    }
}
