using QLN.SearchService.IndexModels;
using QLN.SearchService.Models;

namespace QLN.SearchService.IService
{
    public interface ISearchService
    {
        Task<IEnumerable<ClassifiedIndex>> SearchAsync(SearchRequest request);
        Task<string> UploadAsync(ClassifiedIndex document);
        Task<ClassifiedLandingPageResponse> GetLandingPageDataAsync();

    }
}
