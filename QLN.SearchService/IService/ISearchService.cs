using System.Collections.Generic;
using System.Threading.Tasks;
using QLN.SearchService.IndexModels;
using QLN.SearchService.Models;

namespace QLN.SearchService.IService
{
    public interface ISearchService
    {
        /// <summary>
        /// Full‐text search against the given vertical.
        /// </summary>
        Task<IEnumerable<ClassifiedIndex>> Search(
            string vertical,
            SearchRequest request);

        /// <summary>
        /// Upload (upsert) one item via the CommonIndexRequest wrapper.
        /// </summary>
        Task<string> Upload(CommonIndexRequest request);

        /// <summary>
        /// Fetch a single document by key from the given vertical.
        /// </summary>
        Task<ClassifiedIndex?> GetById(
            string vertical,
            string key);
    }
}
