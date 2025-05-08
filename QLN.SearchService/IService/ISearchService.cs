using QLN.SearchService.IndexModels;

namespace QLN.SearchService.IService
{
    public interface ISearchService
    {
        Task<IEnumerable<ClassifiedIndex>> SearchAsync(SearchRequest request);
        Task<string> UploadAsync(ClassifiedIndex document);

    }
}
