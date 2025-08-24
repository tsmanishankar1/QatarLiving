using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.InstagramDto
{
    public class InstagramPost
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("media_type")]
        public string MediaType { get; set; }
        [JsonPropertyName("media_url")]
        public string MediaUrl { get; set; }
        [JsonPropertyName("caption")]
        public string Caption { get; set; }
        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; }
        [JsonPropertyName("media_product_type")]
        public string MediaProductType { get; set; }
    }

    public class InstagramResponse
    {
        [JsonPropertyName("data")]
        public List<InstagramPost> Data { get; set; } = new();
    }
}
