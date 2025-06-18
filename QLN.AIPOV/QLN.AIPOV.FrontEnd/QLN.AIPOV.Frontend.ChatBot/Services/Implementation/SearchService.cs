using Azure.Search.Documents.Models;
using QLN.AIPOV.Frontend.ChatBot.Services.Interfaces;

namespace QLN.AIPOV.Frontend.ChatBot.Services.Implementation
{
    public class SearchService(
        HttpClient httpClient,
        ILogger<SearchService> logger)
        : ISearchService
    {
        public async Task<SearchResults<SearchDocument>?> KeywordSearchAsync(
            string query,
            int top = 10,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Use relative URL path since BaseAddress is configured in HttpClient
                var url = $"api/Search/keyword?query={Uri.EscapeDataString(query)}&top={top}";
                logger.LogInformation("Making keyword search request to {Url}", url);

                var response = await httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<SearchResults<SearchDocument>>(
                    cancellationToken: cancellationToken);

                return result;
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "Error during keyword search for query '{Query}'", query);
                throw new ApplicationException($"Failed to perform keyword search: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error during keyword search for query '{Query}'", query);
                throw;
            }
        }

        public async Task<SearchResults<SearchDocument>?> VectorSearchAsync(
            string query,
            int top = 10,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Use relative URL path since BaseAddress is configured in HttpClient
                var url = $"api/Search/vector?query={Uri.EscapeDataString(query)}&top={top}";
                logger.LogInformation("Making vector search request to {Url}", url);

                var response = await httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<SearchResults<SearchDocument>>(
                    cancellationToken: cancellationToken);

                return result;
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "Error during vector search for query '{Query}'", query);
                throw new ApplicationException($"Failed to perform vector search: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error during vector search for query '{Query}'", query);
                throw;
            }
        }

        public async Task<SearchResults<SearchDocument>?> HybridSearchAsync(
            string query,
            int top = 10,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Use relative URL path since BaseAddress is configured in HttpClient
                var url = $"api/Search/hybrid?query={Uri.EscapeDataString(query)}&top={top}";
                logger.LogInformation("Making hybrid search request to {Url}", url);

                var response = await httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<SearchResults<SearchDocument>>(
                    cancellationToken: cancellationToken);

                return result;
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "Error during hybrid search for query '{Query}'", query);
                throw new ApplicationException($"Failed to perform hybrid search: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error during hybrid search for query '{Query}'", query);
                throw;
            }
        }
    }
}