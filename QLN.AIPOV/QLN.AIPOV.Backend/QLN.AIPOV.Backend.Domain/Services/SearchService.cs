using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using QLN.AIPOV.Backend.Application.Interfaces;

namespace QLN.AIPOV.Backend.Domain.Services
{
    public class SearchService(
        SearchClient searchClient,
        IEmbeddingService embeddingService)
        : ISearchService
    {
        public async Task<SearchResults<SearchDocument>> KeywordSearchAsync(
            string query,
            int top = 10,
            CancellationToken cancellationToken = default)
        {
            var options = new SearchOptions
            {
                Size = top,
                IncludeTotalCount = true,
                Select = { "FirstName", "LastName", "Email", "MobileNumber", "WorkHistory" }
            };

            return await searchClient.SearchAsync<SearchDocument>(query, options, cancellationToken);
        }

        public async Task<SearchResults<SearchDocument>> VectorSearchAsync(
            string query,
            int top = 10,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Generate vector embeddings for the query
                var queryEmbeddings = await embeddingService.GenerateEmbeddingsAsync(query, cancellationToken);

                // Create search options
                var options = new SearchOptions
                {
                    Size = top,
                    Select = { "FirstName", "LastName", "Email", "MobileNumber" }
                };

                // Add vector search
                options.VectorSearch = new VectorSearchOptions
                {
                    Queries = {
                        new VectorizedQuery(queryEmbeddings.ToArray()) {
                            KNearestNeighborsCount = top,
                            Fields = { "contentVector" }
                        }
                    }
                };

                // Execute vector search (empty string query for pure vector search)
                return await searchClient.SearchAsync<SearchDocument>("", options, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Vector search failed: {ex.Message}. Falling back to keyword search.");
                return await KeywordSearchAsync(query, top, cancellationToken);
            }
        }

        public async Task<SearchResults<SearchDocument>> HybridSearchAsync(
            string query,
            int top = 10,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Generate vector embeddings for the query
                var queryEmbeddings = await embeddingService.GenerateEmbeddingsAsync(query, cancellationToken);

                // Create search options
                var options = new SearchOptions
                {
                    Size = top,
                    Select = { "FirstName", "LastName", "Email", "MobileNumber" },
                    QueryType = SearchQueryType.Full,
                    SemanticSearch = new SemanticSearchOptions
                    {
                        SemanticConfigurationName = "default"
                    }
                };

                // Add vector search
                options.VectorSearch = new VectorSearchOptions
                {
                    Queries = {
                        new VectorizedQuery(queryEmbeddings.ToArray()) {
                            KNearestNeighborsCount = top,
                            Fields = { "contentVector" }
                        }
                    }
                };

                // Execute hybrid search
                return await searchClient.SearchAsync<SearchDocument>(query, options, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hybrid search failed: {ex.Message}. Falling back to keyword search.");
                return await KeywordSearchAsync(query, top, cancellationToken);
            }
        }
    }
}