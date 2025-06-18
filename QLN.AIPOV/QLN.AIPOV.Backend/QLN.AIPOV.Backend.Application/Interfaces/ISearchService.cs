using Azure.Search.Documents.Models;

namespace QLN.AIPOV.Backend.Application.Interfaces
{
    public interface ISearchService
    {
        Task<SearchResults<SearchDocument>> KeywordSearchAsync(
            string query,
            int top = 10,
            CancellationToken cancellationToken = default);

        Task<SearchResults<SearchDocument>> VectorSearchAsync(
            string query,
            int top = 10,
            CancellationToken cancellationToken = default);

        Task<SearchResults<SearchDocument>> HybridSearchAsync(
            string query,
            int top = 10,
            CancellationToken cancellationToken = default);
    }
}