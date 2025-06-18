using Azure.Search.Documents.Models;

namespace QLN.AIPOV.Frontend.ChatBot.Services.Interfaces
{
    /// <summary>
    /// Service interface for interacting with the backend search API
    /// </summary>
    public interface ISearchService
    {
        /// <summary>
        /// Performs a keyword-based text search
        /// </summary>
        /// <param name="query">The text query to search for</param>
        /// <param name="top">Maximum number of results to return</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Search results containing matching documents</returns>
        Task<SearchResults<SearchDocument>?> KeywordSearchAsync(
            string query,
            int top = 10,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs a vector-based semantic search using embeddings
        /// </summary>
        /// <param name="query">The text query to generate embeddings from</param>
        /// <param name="top">Maximum number of results to return</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Search results containing matching documents</returns>
        Task<SearchResults<SearchDocument>?> VectorSearchAsync(
            string query,
            int top = 10,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs a hybrid search combining keyword and vector-based approaches
        /// </summary>
        /// <param name="query">The text query to search for</param>
        /// <param name="top">Maximum number of results to return</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Search results containing matching documents</returns>
        Task<SearchResults<SearchDocument>?> HybridSearchAsync(
            string query,
            int top = 10,
            CancellationToken cancellationToken = default);
    }
}