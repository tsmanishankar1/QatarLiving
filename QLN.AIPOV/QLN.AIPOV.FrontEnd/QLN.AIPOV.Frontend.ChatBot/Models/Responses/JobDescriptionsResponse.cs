using QLN.AIPOV.Frontend.ChatBot.Models.Chat;
using System.Text.Json.Serialization;

namespace QLN.AIPOV.Frontend.ChatBot.Models.Responses
{
    public class JobDescriptionsResponse
    {
        [JsonPropertyName("Message")]
        public JobDescriptions Message { get; set; } = new();
    }
}
