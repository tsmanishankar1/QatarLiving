using QLN.ContentBO.WebUI.Models;
using System.Net.Http;
using System.Threading.Tasks;

namespace QLN.ContentBO.WebUI.Interfaces
{
    public interface IClassifiedService
    {
        /// <summary>
        /// Gets Classifieds by IdCategoryTrees.
        /// </summary>
        /// <param name="vertical">Classifieds CategoryTrees</param>
        /// <returns>HttpResponseMessage</returns>
        Task<HttpResponseMessage?> GetAllCategoryTreesAsync(string vertical);
        Task<HttpResponseMessage> GetServicesCategories(Vertical vertical, SubVertical subVertical);
        Task<HttpResponseMessage?> GetFeaturedSeasonalPicks(Vertical vertical);
        Task<HttpResponseMessage?> GetAllCollectibles(object payload);
        Task<HttpResponseMessage> UpdateCompanyActions(CompanyUpdateActions requestModel);
        Task<HttpResponseMessage?> UpdateCompanyProfile(CompanyProfileItem company);
        Task<HttpResponseMessage?> GetCompanyProfileById(Guid companyId);
        Task<HttpResponseMessage?> PostCollectiblesAdAsync(object payload);
        Task<HttpResponseMessage?> GetFeaturedStores(Vertical vertical);
        Task<HttpResponseMessage?> GetAllSeasonalPicks(Vertical vertical);
        Task<HttpResponseMessage?> GetAllFeaturedStores(Vertical vertical);
        Task<HttpResponseMessage?> CreateSeasonalPicksAsync(object payload);
        Task<HttpResponseMessage?> CreateFeaturedStoresAsync(object payload);
        Task<HttpResponseMessage?> ReplaceSeasonalPickAsync(string pickId, int slot, Vertical vertical);
         Task<HttpResponseMessage?> ReplaceFeaturedStoresAsync(string pickId, int slot, Vertical vertical);
        Task<HttpResponseMessage?> DeleteSeasonalPicks(string pickId, Vertical vertical);
         Task<HttpResponseMessage?> DeleteFeaturedStores(string pickId, Vertical vertical);
        Task<HttpResponseMessage?> ReorderSeasonalPicksAsync(IEnumerable<object> slotAssignments, Vertical vertical);
         Task<HttpResponseMessage?> ReorderFeaturedStoresAsync(IEnumerable<object> slotAssignments, Vertical vertical);
        Task<HttpResponseMessage?> GetFeaturedCategory(Vertical vertical);
        Task<HttpResponseMessage?> GetAllFeatureCategory(Vertical vertical);
        Task<HttpResponseMessage?> CreateFeaturedCategoryAsync(object payload);
        Task<HttpResponseMessage?> UpdateFeaturedCategoryAsync(object payload);
        Task<HttpResponseMessage?> UpdateSeasonalPicksAsync(object payload);
         Task<HttpResponseMessage?> UpdateFeaturedStoreAsync(object payload);
        Task<HttpResponseMessage?> ReplaceFeaturedCategoryAsync(string pickId, int slot, Vertical vertical);
        Task<HttpResponseMessage?> DeleteFeaturedCategory(string pickId, Vertical vertical);
        Task<HttpResponseMessage?> ReorderFeaturedCategoryAsync(IEnumerable<object> slotAssignments, Vertical vertical);
        Task<HttpResponseMessage?> GetPrelovedListingsAsync(FilterRequest request);
        Task<List<HttpResponseMessage>> SearchClassifiedsViewListingAsync(string vertical, object searchPayload);
        Task<HttpResponseMessage?> PerformBulkActionAsync(object payload);
        Task<HttpResponseMessage?> GetPrelovedSubscription(FilterRequest request);
        Task<HttpResponseMessage?> GetPrelovedP2pTransaction(FilterRequest request);
        Task<HttpResponseMessage?> GetPrelovedUserListing(FilterRequest request);
        Task<HttpResponseMessage?> GetPrelovedP2pListing(FilterRequest request);
        Task<HttpResponseMessage?> PerformPrelovedBulkActionAsync(object payload);

        Task<List<HttpResponseMessage>> SearchClassifiedsViewTransactionAsync(object searchPayload);
        Task<HttpResponseMessage?> PerformBulkActionAsync(string vertical, object payload);
        Task<HttpResponseMessage?> GetAdByIdAsync(long adId);
        Task<HttpResponseMessage?> GetCollectibleIdAsync(long adId);
        Task<HttpResponseMessage?> GetAllZonesAsync();

        /// <summary>
        /// Gets the address coordinates by zone, street, building, and location.
        Task<HttpResponseMessage?> GetAddressByDetailsAsync(int zone, int street, int building, string location);
        /// <summary>
        /// Posts a new classified ad.
        Task<HttpResponseMessage?> PostAdAsync(string vertical, object payload);
        Task<HttpResponseMessage?> UpdateAdAsync(string vertical, object payload);
        Task<HttpResponseMessage?> UplodAsync(object payload);
        Task<HttpResponseMessage?> RefreshAdAsync(long adId, int subVertical);

        //Deals
        Task<HttpResponseMessage?> GetDealsSubscription(FilterRequest request);
        Task<HttpResponseMessage?> GetDealsListing(FilterRequest request);
        Task<HttpResponseMessage?> PerformDealsBulkActionAsync(object payload);
        Task<HttpResponseMessage?> GetDealsByIdAsync(string vertical, long? adId);
        Task<HttpResponseMessage?> UpdateDealsAsync(object payload);
        Task<HttpResponseMessage> GetFeaturedCategoryById(string id);
        Task<HttpResponseMessage> GetFeaturedStoreById(string id);
        Task<HttpResponseMessage> GetSeasonalPicksById(string id);
    }
}
