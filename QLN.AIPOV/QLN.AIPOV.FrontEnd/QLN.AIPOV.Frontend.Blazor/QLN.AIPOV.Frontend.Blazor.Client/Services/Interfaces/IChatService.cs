using QLN.AIPOV.Frontend.Blazor.Client.Models;

namespace QLN.AIPOV.Frontend.Blazor.Client.Services.Interfaces
{
    public interface IChatService
    {
        Task<IEnumerable<ChatMessageModel>> GetMessagesAsync(string prompt);
    }
}
