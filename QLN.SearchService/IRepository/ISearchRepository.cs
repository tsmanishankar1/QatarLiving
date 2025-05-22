using System.Collections.Generic;
using System.Threading.Tasks;
using QLN.SearchService.Models;

namespace QLN.SearchService.IRepository
{
    public interface ISearchRepository
    {
        Task<IEnumerable<T>> Search<T>(string vertical, SearchRequest req);
        Task<string> Upload<T>(string vertical, T doc);
        Task<T?> GetById<T>(string vertical, string key);
    }
}
