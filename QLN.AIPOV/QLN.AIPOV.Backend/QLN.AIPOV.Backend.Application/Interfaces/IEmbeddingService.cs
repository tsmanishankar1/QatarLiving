namespace QLN.AIPOV.Backend.Application.Interfaces
{
    public interface IEmbeddingService
    {
        /// <summary>
        /// Generates vector embeddings for the given text
        /// </summary>
        /// <param name="text">Text to convert to embeddings</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Array of embedding values</returns>
        Task<IReadOnlyList<float>> GenerateEmbeddingsAsync(string text, CancellationToken cancellationToken = default);
    }
}