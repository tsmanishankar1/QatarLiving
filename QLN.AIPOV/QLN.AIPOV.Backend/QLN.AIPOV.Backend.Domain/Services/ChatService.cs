using QLN.AIPOV.Backend.Application.Interfaces;

namespace QLN.AIPOV.Backend.Domain.Services
{
    public class ChatService(IChatGPTClient chatGPTClient) : IChatService
    {
        public async Task<string> GetChatResponseAsync(string prompt, CancellationToken cancellationToken = default)
        {
            var response = await chatGPTClient.GetChatResponseAsync(prompt, cancellationToken);

            return response;
        }
    }
}
