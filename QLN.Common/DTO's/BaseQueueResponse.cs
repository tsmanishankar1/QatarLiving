using System.Text.Json.Serialization;

namespace QLN.Common.Infrastructure.DTO_s
{
    public class BaseQueueResponse<T>
    {
        [JsonPropertyName("queue_label")]
        public string QueueLabel { get; set; } = string.Empty;
        [JsonPropertyName("items")]
        public List<T> Items { get; set; } = new List<T>();
    }
}
