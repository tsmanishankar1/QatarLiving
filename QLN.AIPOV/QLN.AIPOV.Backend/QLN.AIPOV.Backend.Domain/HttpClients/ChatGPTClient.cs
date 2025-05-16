using Azure.AI.OpenAI;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using QLN.AIPOV.Backend.Application.Interfaces;
using QLN.AIPOV.Backend.Application.Models.Config;

namespace QLN.AIPOV.Backend.Domain.HttpClients
{
    public class ChatGPTClient(AzureOpenAIClient openAIClient, IOptions<OpenAISettingsModel> openAISettings) : IChatGPTClient
    {
        private readonly OpenAISettingsModel _openAISettings = openAISettings.Value;

        public async Task<List<string>> GetChatResponseAsync(string prompt, CancellationToken cancellationToken = default)
        {
            var chatClient = openAIClient.GetChatClient(_openAISettings.Model);

            var messages = new List<ChatMessage>()
            {
                new SystemChatMessage(_openAISettings.SystemPrompt),
                new UserChatMessage(prompt)
            };

            var chatCompletion = await chatClient.CompleteChatAsync(messages.ToArray());

            return chatCompletion.Value.Content.Select(x => x.Text).ToList();
        }
    }
}
