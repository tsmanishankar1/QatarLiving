using System.Text.Json.Serialization;

namespace QLN.Common.Infrastructure.DTO_s
{
    public class ContentBase
    { 
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("page_name")]
        public string PageName { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("queue_name")]
        public string QueueName { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("queue_label")]
        public string QueueLabel { get; set; }
        public Guid? QueueLabelId { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("node_type")]
        public string NodeType { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("slug")]
        public string Slug { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("comment_count")]
        public string? CommentCount { get; set; }
    }
}
