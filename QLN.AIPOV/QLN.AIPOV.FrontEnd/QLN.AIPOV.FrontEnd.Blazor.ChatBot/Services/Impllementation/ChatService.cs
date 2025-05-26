using QLN.AIPOV.FrontEnd.Blazor.ChatBot.Models;
using QLN.AIPOV.FrontEnd.Blazor.ChatBot.Services.Interfaces;

namespace QLN.AIPOV.FrontEnd.Blazor.ChatBot.Services.Impllementation
{
    public class ChatService(HttpClient httpClient) : ServiceBase(httpClient), IChatService
    {
        public async Task<IEnumerable<ChatMessageModel>> GetMessagesAsync(string prompt)
        {
            var chatMessage = new ChatMessageModel
            {
                Role = "User",
                Content = prompt
            };
            var response = await PostAsync<ChatMessageModel, ChatSessionModel>("api/chat", chatMessage);

            return response?.Messages ?? new List<ChatMessageModel>();
        }
    }
}
