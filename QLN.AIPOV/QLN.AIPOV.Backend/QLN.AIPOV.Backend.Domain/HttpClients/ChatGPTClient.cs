using Azure.AI.OpenAI;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using QLN.AIPOV.Backend.Application.Interfaces;
using QLN.AIPOV.Backend.Application.Models.Chat;
using QLN.AIPOV.Backend.Application.Models.Config;
using QLN.AIPOV.Backend.Infrastructure.Extensions;
using System.Text.Json;

namespace QLN.AIPOV.Backend.Domain.HttpClients
{
    public class ChatGPTClient(AzureOpenAIClient openAIClient, IOptions<OpenAISettingsModel> openAISettings) : IChatGPTClient
    {
        private readonly OpenAISettingsModel _openAISettings = openAISettings.Value;

        public async Task<JobDescriptions> GetChatResponseAsync(string prompt, CancellationToken cancellationToken = default)
        {
            var chatClient = openAIClient.GetChatClient(_openAISettings.Model);

            var schema = JsonExtensions.GenerateJsonSchema<JobDescriptions>();

            var requestOptions = new ChatCompletionOptions
            {
                ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(nameof(JobDescriptions), BinaryData.FromString(schema), null, true)
            };

            var messages = new List<ChatMessage>()
            {
                new SystemChatMessage(_openAISettings.SystemPrompt),
                new UserChatMessage(prompt)
            };

            var chatCompletion = await chatClient.CompleteChatAsync(messages, requestOptions, cancellationToken);

            var data = chatCompletion.Value.Content[0].Text;

            var jobDescriptions = JsonSerializer.Deserialize<JobDescriptions>(data);

            return jobDescriptions ?? new JobDescriptions { Descriptions = new List<JobDescription>() }; // or handle the error as needed
        }
    }
}
