using QLN.ContentBO.WebUI.Models;

namespace QLN.ContentBO.WebUI.Interfaces
{
    public interface IDealsService
    {
        Task<HttpResponseMessage> GetAllDealsSubscription(DealsSubscriptionQuery query);
        Task<HttpResponseMessage> GetAllDealsListing(CompanySubscriptionFilter companyRequestPayload);
        Task<HttpResponseMessage?> BulkActionAsync(List<long?> adIds, int action, string? reason = null, string? comments = null);
    }
}
