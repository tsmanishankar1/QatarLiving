using QLN.SearchService.IndexModels;
using QLN.SearchService.Models;

namespace QLN.SearchService.IRepository
{
    public interface ISearchRepository
    {
        Task<IEnumerable<ClassifiedIndex>> SearchAsync(SearchRequest request);
        Task<string> UploadAsync(ClassifiedIndex document);
        Task<IEnumerable<ClassifiedIndex>> GetFeaturedItemsAsync();
        Task<IEnumerable<LandingCategoryInfo>> GetFeaturedCategoriesAsync();
        Task<IEnumerable<CategoryAdCount>> GetCategoryAdCountsAsync();
        Task<IEnumerable<LandingStoreInfo>> GetStoresWithCountsAsync();
    }
}
