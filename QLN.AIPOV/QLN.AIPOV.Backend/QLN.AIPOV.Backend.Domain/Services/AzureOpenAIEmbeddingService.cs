using Azure.AI.OpenAI;
using Microsoft.Extensions.Options;
using OpenAI.Embeddings;
using QLN.AIPOV.Backend.Application.Interfaces;
using QLN.AIPOV.Backend.Application.Models.Config;

namespace QLN.AIPOV.Backend.Domain.Services
{
    public class AzureOpenAIEmbeddingService : IEmbeddingService
    {
        private readonly AzureOpenAIClient _openAIClient;
        private readonly string _embeddingModel;

        public AzureOpenAIEmbeddingService(
            AzureOpenAIClient openAIClient,
            IOptions<OpenAISettingsModel> openAISettings)
        {
            _openAIClient = openAIClient ?? throw new ArgumentNullException(nameof(openAIClient));
            var settings = openAISettings.Value;

            // Use the configured model
            _embeddingModel = settings.EmbeddingModel;
        }

        public async Task<IReadOnlyList<float>> GenerateEmbeddingsAsync(
            string text,
            CancellationToken cancellationToken = default)
        {
            // Truncate text if too long to fit token limits
            if (text.Length > 8000)
            {
                text = text.Substring(0, 8000);
            }

            try
            {
                // Get embeddings using the embedded client from AzureOpenAIClient
                var embeddingClient = _openAIClient.GetEmbeddingClient(_embeddingModel);

                // Create embedding options with properly created input
                var inputString = new List<string> { text };

                // Request embeddings
                var response = await embeddingClient.GenerateEmbeddingsAsync(inputString, new EmbeddingGenerationOptions(), cancellationToken);

                // Extract the embedding data - check the actual structure in debugging
                // The return value might be response.Value.Data[0].Embedding based on the Azure SDK
                if (response.Value.GetType().GetProperty("Data") != null)
                {
                    // If using Azure.AI.OpenAI structure
                    dynamic data = response.Value;
                    return data.Data[0].Embedding.ToArray();
                }

                // Access the embedding vector directly if different structure
                return ((IEnumerable<float>)response.Value.GetType()
                            .GetProperty("Embedding")?.GetValue(response.Value, null)!
                    ?? Array.Empty<float>()).ToArray();
            }
            catch (Exception ex)
            {
                // Log error and rethrow
                Console.WriteLine($"Error generating embeddings: {ex.Message}");
                throw;
            }
        }
    }
}