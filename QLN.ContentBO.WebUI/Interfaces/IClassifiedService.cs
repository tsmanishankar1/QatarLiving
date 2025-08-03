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
        Task<HttpResponseMessage?> GetFeaturedSeasonalPicks(string vertical);
        Task<HttpResponseMessage?> GetAllSeasonalPicks(string vertical);
        Task<HttpResponseMessage?> CreateSeasonalPicksAsync(object payload);
        Task<HttpResponseMessage?> ReplaceSeasonalPickAsync(string pickId, int slot, string vertical);
        Task<HttpResponseMessage?> DeleteSeasonalPicks(string pickId, string vertical);
        Task<HttpResponseMessage?> ReorderSeasonalPicksAsync(IEnumerable<object> slotAssignments,  string vertical);
        Task<HttpResponseMessage?> GetFeaturedCategory(string vertical);
        Task<HttpResponseMessage?> GetAllFeatureCategory(string vertical);
        Task<HttpResponseMessage?> CreateFeaturedCategoryAsync(object payload);
        Task<HttpResponseMessage?> ReplaceFeaturedCategoryAsync(string pickId, int slot, string vertical);
        Task<HttpResponseMessage?> DeleteFeaturedCategory(string pickId, string vertical);
        Task<HttpResponseMessage?> ReorderFeaturedCategoryAsync(IEnumerable<object> slotAssignments, string vertical);
        Task<HttpResponseMessage?> GetPrelovedListingsAsync(FilterRequest request);
        Task<List<HttpResponseMessage>> SearchClassifiedsViewListingAsync(string vertical, object searchPayload);
        Task<HttpResponseMessage?> PerformBulkActionAsync(object payload);
        Task<HttpResponseMessage?> GetPrelovedSubscription(FilterRequest request);
        Task<HttpResponseMessage?> GetPrelovedP2pTransaction(FilterRequest request);
        Task<HttpResponseMessage?> GetPrelovedUserListing(FilterRequest request);
        Task<HttpResponseMessage?> GetPrelovedP2pListing(FilterRequest request);
        Task<HttpResponseMessage?> PerformPrelovedBulkActionAsync(object payload);


    }
}
