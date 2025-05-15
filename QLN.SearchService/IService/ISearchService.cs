using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Search.Documents.Models;
using QLN.SearchService.Models;

namespace QLN.SearchService.IService
{
    public interface ISearchService
    {
        Task<IEnumerable<SearchDocument>> SearchAsync(string vertical, SearchRequest request);
        Task<string> UploadAsync(string vertical, SearchDocument document);
        Task<object> GetByIdAsync(string vertical, string key);
    }
}
