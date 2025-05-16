namespace QLN.AIPOV.Backend.Application.Interfaces
{
    public interface IChatGPTClient
    {
        Task<List<string>> GetChatResponseAsync(string prompt, CancellationToken cancellationToken);
    }
}
