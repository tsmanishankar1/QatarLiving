using QLN.SearchService.IndexModels;

namespace QLN.SearchService.IRepository
{
    public interface ISearchRepository
    {
        Task<IEnumerable<ClassifiedIndex>> SearchAsync(SearchRequest request);
        Task<string> UploadAsync(ClassifiedIndex document);
    }
}
