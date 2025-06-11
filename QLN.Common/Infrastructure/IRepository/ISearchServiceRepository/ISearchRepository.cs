using Azure.Search.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IRepository.ISearchServiceRepository
{
    public interface ISearchRepository
    {
        Task<IEnumerable<T>> SearchAsync<T>(string vertical, SearchOptions options, string searchText);
        Task<string> UploadAsync<T>(string vertical, T document);
        Task<T?> GetByIdAsync<T>(string vertical, string key);
        Task DeleteAsync(string vertical, string key);

    }
}
