using Microsoft.Extensions.Options;
using QLN.AIPOV.Backend.Application.Interfaces;
using QLN.AIPOV.Backend.Application.Models.Config;
using System.Net.Http.Json;
using System.Text.Json;

namespace QLN.AIPOV.Backend.Domain.HttpClients
{
    public class ChatGPTClient(HttpClient httpClient,
        IOptions<OpenAISettingsModel> openAISettings) : IChatGPTClient
    {
        private readonly OpenAISettingsModel _openAISettings = openAISettings.Value;
        public async Task<string> GetChatResponseAsync(string prompt, CancellationToken cancellationToken = default)
        {
            var requestBody = new
            {
                model = _openAISettings.Model,
                messages = new[]
                {
                    new { role = "system", content = "You are a helpful assistant that creates job specs." },
                    new { role = "user", content = prompt }
                },
                max_tokens = 200
            };

            var response = await httpClient.PostAsJsonAsync("", requestBody, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken);

            var assistantMessage = responseJson!
                .RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;

            return assistantMessage;
        }
    }
}
