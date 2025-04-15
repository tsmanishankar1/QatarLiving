using Azure.Search.Documents.Models;
using Azure.Search.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Indexing.IService
{
    public interface ISearchService<T>
    {
        Task CreateIndexIfNotExistsAsync(string indexName, CancellationToken cancellationToken = default);
        Task CreateOrUpdateIndexAsync(string indexName, CancellationToken cancellationToken = default);

        Task UploadDocumentsAsync(string indexName, IEnumerable<T> documents, CancellationToken cancellationToken = default);

        Task<SearchResults<T>> SearchAsync(string indexName, string searchText, SearchOptions? options = null, CancellationToken cancellationToken = default);
    }
}
