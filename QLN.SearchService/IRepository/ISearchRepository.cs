using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;

namespace QLN.SearchService.IRepository
{
    public interface ISearchRepository
    {
        Task<IEnumerable<T>> SearchAsync<T>(string vertical, SearchOptions options, string searchText);
        Task<string> UploadAsync<T>(string vertical, T document);
        Task<T?> GetByIdAsync<T>(string vertical, string key);
    }
}
