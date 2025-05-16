using QLN.AIPOV.Backend.Application.Interfaces;
using QLN.AIPOV.Backend.Application.Models.Chat;

namespace QLN.AIPOV.Backend.Domain.Services
{
    public class ChatService(IChatGPTClient chatGPTClient) : IChatService
    {
        public async Task<ChatSessionModel> GetChatResponseAsync(string prompt, CancellationToken cancellationToken = default)
        {
            var response = await chatGPTClient.GetChatResponseAsync(prompt, cancellationToken);

            return response;
        }
    }
}
