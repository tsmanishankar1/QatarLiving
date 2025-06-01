using System.Text.Json.Serialization;

namespace QLN.Common.Infrastructure.DTO_s
{
    public class ContentVideo : ContentBase
    {
        [JsonPropertyName("nid")]
        public string Nid { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("date_created")]
        public string DateCreated { get; set; }

        [JsonPropertyName("image_url")]
        public string ImageUrl { get; set; }

        [JsonPropertyName("video_url")]
        public string VideoUrl { get; set; }

        [JsonPropertyName("user_name")]
        public string UserName { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

    }
}
