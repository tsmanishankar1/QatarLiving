using Azure.Search.Documents.Models;

namespace QLN.AIPOV.Backend.Application.Interfaces
{
    public interface ISearchService
    {
        Task<List<SearchDocument>> KeywordSearchAsync(
            string query,
            int top = 10,
            CancellationToken cancellationToken = default);

        Task<List<SearchDocument>> VectorSearchAsync(
            string query,
            int top = 10,
            CancellationToken cancellationToken = default);

        Task<List<SearchDocument>> HybridSearchAsync(
            string query,
            int top = 10,
            CancellationToken cancellationToken = default);
    }
}