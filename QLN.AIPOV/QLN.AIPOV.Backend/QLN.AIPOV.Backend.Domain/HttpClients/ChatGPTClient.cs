using Azure.AI.OpenAI;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using QLN.AIPOV.Backend.Application.Interfaces;
using QLN.AIPOV.Backend.Application.Models.Chat;
using QLN.AIPOV.Backend.Application.Models.Config;

namespace QLN.AIPOV.Backend.Domain.HttpClients
{
    public class ChatGPTClient(AzureOpenAIClient openAIClient, IOptions<OpenAISettingsModel> openAISettings) : IChatGPTClient
    {
        private readonly OpenAISettingsModel _openAISettings = openAISettings.Value;

        public async Task<ChatSessionModel> GetChatResponseAsync(string prompt, CancellationToken cancellationToken = default)
        {
            var chatClient = openAIClient.GetChatClient(_openAISettings.Model);

            var messages = new List<ChatMessage>()
            {
                new SystemChatMessage(_openAISettings.SystemPrompt),
                new UserChatMessage(prompt)
            };

            var chatCompletion = await chatClient.CompleteChatAsync(messages.ToArray());

            var session = new ChatSessionModel();
            // Add user message
            session.Messages.Add(new ChatMessageModel { Role = "user", Content = prompt });

            // Add assistant responses
            foreach (var choice in chatCompletion.Value.Content)
            {
                session.Messages.Add(new ChatMessageModel
                {
                    Role = "assistant",
                    Content = choice.Text
                });
            }

            return session;
        }
    }
}
