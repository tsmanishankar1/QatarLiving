using System.Text.Json.Serialization;

namespace QLN.AIPOV.Frontend.ChatBot.Models.Chat
{
    public class JobDescriptions
    {
        [JsonPropertyName("Job_Descriptions")]
        public List<JobDescription>? Descriptions { get; set; } = new List<JobDescription>();
    }
}
