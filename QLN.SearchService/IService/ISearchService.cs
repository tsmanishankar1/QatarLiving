using System.Collections.Generic;
using System.Threading.Tasks;
using QLN.SearchService.IndexModels;
using QLN.SearchService.Models;

namespace QLN.SearchService.IService
{
    public interface ISearchService
    {
        Task<IEnumerable<ClassifiedsIndex>> SearchAsync(string vertical, SearchRequest request);
        Task<string> UploadAsync(CommonIndexRequest request);
        Task<ClassifiedsIndex?> GetByIdAsync(string vertical, string key);
    }
}
