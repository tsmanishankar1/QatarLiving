using QLN.AIPOV.Backend.Application.Models.Chat;

namespace QLN.AIPOV.Backend.Application.Interfaces
{
    public interface IChatService
    {
        Task<ChatSessionModel> GetChatResponseAsync(string prompt, CancellationToken cancellationToken);
    }
}
