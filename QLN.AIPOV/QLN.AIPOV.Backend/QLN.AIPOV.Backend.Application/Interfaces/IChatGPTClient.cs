namespace QLN.AIPOV.Backend.Application.Interfaces
{
    public interface IChatGPTClient
    {
        Task<string> GetChatResponseAsync(string prompt, CancellationToken cancellationToken = default);
    }
}
