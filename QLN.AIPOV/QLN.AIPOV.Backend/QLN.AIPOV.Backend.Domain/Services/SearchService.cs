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
        public async Task<List<SearchDocument>> KeywordSearchAsync(
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

            var results = await searchClient.SearchAsync<SearchDocument>(query, options, cancellationToken);
            var documents = results.Value.GetResults().Select(r => r.Document).ToList();
            return documents;
        }

        public async Task<List<SearchDocument>> VectorSearchAsync(
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
                            Fields = { "skillsVector" }
                        }
                    }
                };

                // Execute vector search (empty string query for pure vector search)
                var results = await searchClient.SearchAsync<SearchDocument>(string.Empty, options, cancellationToken);
                return results.Value.GetResults().Select(r => r.Document).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Vector search failed: {ex.Message}. Falling back to keyword search.");
                return await KeywordSearchAsync(query, top, cancellationToken);
            }
        }

        public async Task<List<SearchDocument>> HybridSearchAsync(
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
                var results = await searchClient.SearchAsync<SearchDocument>(query, options, cancellationToken);
                return results.Value.GetResults().Select(r => r.Document).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hybrid search failed: {ex.Message}. Falling back to keyword search.");
                return await KeywordSearchAsync(query, top, cancellationToken);
            }
        }
    }
}