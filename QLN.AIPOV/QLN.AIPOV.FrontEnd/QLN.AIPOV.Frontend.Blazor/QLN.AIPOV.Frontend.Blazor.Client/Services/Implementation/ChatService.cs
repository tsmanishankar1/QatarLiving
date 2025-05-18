using QLN.AIPOV.Frontend.Blazor.Client.Models;
using QLN.AIPOV.Frontend.Blazor.Client.Services.Interfaces;

namespace QLN.AIPOV.Frontend.Blazor.Client.Services.Implementation
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
