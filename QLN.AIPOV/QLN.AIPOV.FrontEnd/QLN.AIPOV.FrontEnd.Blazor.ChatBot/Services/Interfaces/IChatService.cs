using QLN.AIPOV.FrontEnd.Blazor.ChatBot.Models;

namespace QLN.AIPOV.FrontEnd.Blazor.ChatBot.Services.Interfaces
{
    public interface IChatService
    {
        Task<IEnumerable<ChatMessageModel>> GetMessagesAsync(string prompt);
    }
}
