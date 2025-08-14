using System.Text.Json.Serialization;

namespace QLN.Common.DTO_s.Implio
{
    public class ImplioItem
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("batchId")]
        public string BatchId { get; set; }

        [JsonPropertyName("taskId")]
        public string TaskId { get; set; }
    }
}
