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
        Task<HttpResponseMessage?> ReorderSeasonalPicksAsync(IEnumerable<object> slotAssignments, string vertical);
        Task<HttpResponseMessage?> GetFeaturedCategory(string vertical);
        Task<HttpResponseMessage?> GetAllFeatureCategory(string vertical);
        Task<HttpResponseMessage?> CreateFeaturedCategoryAsync(object payload);
        Task<HttpResponseMessage?> ReplaceFeaturedCategoryAsync(string pickId, int slot, string vertical);
        Task<HttpResponseMessage?> DeleteFeaturedCategory(string pickId, string vertical);
        Task<HttpResponseMessage?> ReorderFeaturedCategoryAsync(IEnumerable<object> slotAssignments, string vertical);
        Task<HttpResponseMessage?> GetPrelovedListingsAsync(FilterRequest request);
        Task<List<HttpResponseMessage>> SearchClassifiedsViewListingAsync(string vertical, object searchPayload);
        Task<List<HttpResponseMessage>> SearchClassifiedsViewTransactionAsync(object searchPayload);
        Task<HttpResponseMessage?> PerformBulkActionAsync(string vertical,object payload);
        Task<HttpResponseMessage?> GetAdByIdAsync(string vertical, string adId);

        Task<HttpResponseMessage?> GetAllZonesAsync();

        /// <summary>
        /// Gets the address coordinates by zone, street, building, and location.
        Task<HttpResponseMessage?> GetAddressByDetailsAsync(int zone, int street, int building, string location);
        /// <summary>
        /// Posts a new classified ad.
        Task<HttpResponseMessage?> PostAdAsync(string vertical, object payload);
        Task<HttpResponseMessage?> UplodAsync(object payload);


    }
}
