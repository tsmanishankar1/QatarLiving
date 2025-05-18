using QLN.AIPOV.Frontend.ChatBot.Models.Chat;

namespace QLN.AIPOV.Frontend.ChatBot.Models.Responses
{
    public class ChatCompletionResponse
    {
        public ChatSessionModel Message { get; set; } = new();
    }
}
