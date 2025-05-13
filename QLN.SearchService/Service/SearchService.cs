using QLN.SearchService.IndexModels;
using QLN.SearchService.IRepository;
using QLN.SearchService.IService;
using QLN.SearchService.Models;

namespace QLN.SearchService.Service
{
    public class SearchService : ISearchService
    {
        private readonly ISearchRepository _repository;

        public SearchService(ISearchRepository repository)
        {
            _repository = repository;
        }

        public Task<IEnumerable<ClassifiedIndex>> SearchAsync(SearchRequest request)
            => _repository.SearchAsync(request);

        public Task<string> UploadAsync(ClassifiedIndex document)
        => _repository.UploadAsync(document);
        public async Task<ClassifiedLandingPageResponse> GetLandingPageDataAsync()
        {
            var featuredItems = await _repository.GetFeaturedItemsAsync();
            var featuredCategories = await _repository.GetFeaturedCategoriesAsync();
            var categoriesCount = await _repository.GetCategoryAdCountsAsync();
            var stores = await _repository.GetStoresWithCountsAsync();

            return new ClassifiedLandingPageResponse
            {
                FeaturedItems = featuredItems,
                FeaturedCategories = featuredCategories,
                CategoryAdCounts = categoriesCount,
                FeaturedStores = stores
            };
        }
    }
}
