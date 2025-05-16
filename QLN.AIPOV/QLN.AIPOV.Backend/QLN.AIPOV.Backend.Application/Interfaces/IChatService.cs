namespace QLN.AIPOV.Backend.Application.Interfaces
{
    public interface IChatService
    {
        Task<string> GetChatResponseAsync(string prompt, CancellationToken cancellationToken = default);
    }
}
