using System.Text.Json.Serialization;

namespace QLN.Common.Infrastructure.DTO_s
{
    public class ContentPost : ContentBase
    {
        [JsonPropertyName("nid")]
        public string Nid { get; set; }

        [JsonPropertyName("date_created")]
        public string DateCreated { get; set; }

        [JsonPropertyName("image_url")]
        public string ImageUrl { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

    }
}
