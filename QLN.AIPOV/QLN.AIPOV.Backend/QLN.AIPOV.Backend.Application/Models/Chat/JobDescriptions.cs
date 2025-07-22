using System.Text.Json.Serialization;

namespace QLN.AIPOV.Backend.Application.Models.Chat
{
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public class JobDescriptions
    {
        [JsonPropertyName("Job_Descriptions")]
        public required IEnumerable<JobDescription> Descriptions { get; set; }
    }
}
