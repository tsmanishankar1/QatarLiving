using System.Text.Json.Serialization;

namespace QLN.Common.DTO_s.Implio
{
    public class ImplioResult
    {
        [JsonPropertyName("actorId")]
        public string ActorId { get; set; } = string.Empty;

        [JsonPropertyName("outcome")]
        public string Outcome { get; set; } = string.Empty;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("reasons")]
        public List<ImplioReason>? Reasons { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("feedback")]
        public List<ImplioFeedback>? Feedback { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("matchingFilters")]
        public List<ImplioMatchingFilters>? MatchingFilters { get; set; }
    }
}
