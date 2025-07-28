using Azure.Search.Documents;
using QLN.Common.DTO_s;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IRepository.ISearchServiceRepository
{
    public interface ISearchRepository
    {
        Task<AzureSearchResults<T>> SearchAsync<T>(string IndexName, SearchOptions options, string searchText);
        Task<string> UploadAsync<T>(string IndexName, T document);
        Task<T?> GetByIdAsync<T>(string IndexName, string key);
        Task DeleteAsync(string IndexName, string key);
    }
}
