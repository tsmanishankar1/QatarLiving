using Azure.Search.Documents.Models;
using QLN.SearchService.IndexModels;
using QLN.SearchService.Models;

namespace QLN.SearchService.IRepository
{
    public interface ISearchRepository
    {
        Task<IEnumerable<T>> SearchAsync<T>(string vertical, SearchRequest req);
        Task<string> UploadAsync<T>(string vertical, T doc);

        Task<string> UploadAsync(string vertical, SearchDocument doc)
            => UploadAsync<SearchDocument>(vertical, doc);
    }
}
