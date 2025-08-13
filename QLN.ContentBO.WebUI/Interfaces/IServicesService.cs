using QLN.ContentBO.WebUI.Models;
using static QLN.ContentBO.WebUI.Pages.NewsPage.NewsBase;
namespace QLN.ContentBO.WebUI.Interfaces
{
    public interface IServiceBOService
    {
        Task<HttpResponseMessage> GetServicesCategories();
        Task<HttpResponseMessage> GetAllClassifiedsCategories(Vertical vertical,SubVertical subVertical);
        Task<HttpResponseMessage> GetPaginatedP2PListing(
    string? sortBy = null,
    string? search = null,
    DateTime? fromDate = null,
    DateTime? toDate = null,
    DateTime? publishedFrom = null,
    DateTime? publishedTo = null,
    int? status = null,
    bool? isPromoted = null,
    bool? isFeatured = null,
    int? pageNumber = null,
    int? pageSize = null);
        Task<HttpResponseMessage> GetPaginatedSubscriptionListing(
    string? sortBy = null,
    string? search = null,
    DateTime? fromDate = null,
    DateTime? toDate = null,
    int? pageNumber = null,
    int? pageSize = null,
    string? subscriptionType = null);

        Task<HttpResponseMessage> GetPaginatedP2PTransactionListing(
        string? sortBy = null,
        string? search = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int? pageNumber = null,
        int? pageSize = null);
        Task<HttpResponseMessage> GetServiceById(long id);
        Task<HttpResponseMessage?> GetAllZonesAsync();
        Task<HttpResponseMessage> UpdateService(ServicesDto service);
        Task<HttpResponseMessage> ModerateBulkAction(object payload);
        Task<HttpResponseMessage> GetAllCompaniesAsync(object payload);
        Task<HttpResponseMessage> GetPaginatedSubscriptionAdsListing(
        string? sortBy = null,
        string? search = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        DateTime? publishedFrom = null,
        DateTime? publishedTo = null,
        int? status = null,
        bool? isPromoted = null,
        bool? isFeatured = null,
        int? pageNumber = null,
        int? pageSize = null);
        Task<HttpResponseMessage> UpdateServiceStatus(BulkModerationRequest requestModel);
    }
}
