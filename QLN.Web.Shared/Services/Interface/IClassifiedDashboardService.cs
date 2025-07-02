using static QLN.Web.Shared.Models.ClassifiedsDashboardModel;

namespace QLN.Web.Shared.Services.Interface
{
    public interface IClassifiedDashboardService
    {
        Task<ItemDashboardResponse?> GetItemDashboard();
        Task<PreLovedDashboardResponse?> GetPreLovedDashboard();
        Task<List<AdModal>?> GetPublishedAds( int page, int pageSize, string search, int sortOption);
        Task<List<AdModal>?> GetUnpublishedAds( int page, int pageSize, string search, int sortOption);
        Task<List<AdModal>?> GetPreLovedPublishedAds(int page, int pageSize, string search, int sortOption);
        Task<List<AdModal>?> GetPreLovedUnPublishedAds(int page, int pageSize, string search, int sortOption);
        Task<List<AdModal>?> GetCollectiblesPublishedAds(int page, int pageSize, string search, int sortOption);
        Task<List<AdModal>?> GetCollectiblesUnPublishedAds(int page, int pageSize, string search, int sortOption);
        Task<List<AdModal>?> GetStoresPublishedAds(int page, int pageSize, string search, int sortOption);
        Task<List<AdModal>?> GetStoresUnPublishedAds(int page, int pageSize, string search, int sortOption);
        Task<List<AdModal>?> GetDealsPublishedAds(int page, int pageSize, string search, int sortOption);
        Task<List<AdModal>?> GetDealsUnPublishedAds(int page, int pageSize, string search, int sortOption);
        Task<bool> PublishAdAsync(List<string> adId);
        Task<bool> UnPublishAdAsync(List<string> adId);
        Task<bool> PublishPreLovedAdAsync(List<string> adId);
        Task<bool> UnPublishPreLovedAdAsync(List<string> adId);
        Task<bool> PublishCollectiblesAdAsync(List<string> adId);
        Task<bool> UnPublishCollectiblesAdAsync(List<string> adId);
        Task<bool> PublishDealsAdAsync(List<string> adId);
        Task<bool> UnPublishDealsAdAsync(List<string> adId);
        Task<bool> RemoveItemAdAsync(string adId);
        Task<bool> RemoveCollectiblesAdAsync(string adId);
        Task<bool> RemovePrelovedAsync(string adId);
        Task<bool> RemoveDealsAdAsync(string adId);


    }
}
