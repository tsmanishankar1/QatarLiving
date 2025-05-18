using QLN.AIPOV.Frontend.ChatBot.Models.Responses;

namespace QLN.AIPOV.FrontEnd.ChatBot.Services.Interfaces
{
    public interface IChatService
    {
        Task<ChatCompletionResponse> GetMessagesAsync(string prompt);
    }
}
