using System.Text.Json.Serialization;

namespace QLN.Common.DTO_s.Implio
{
    public class ImplioMatchingFilters
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("vote")]
        public string Vote { get; set; }

        [JsonPropertyName("wordHighlighting")]
        public ImplioWordHighlighting WordHighlighting { get; set; }

        [JsonPropertyName("notice")]
        public ImplioNotice Notice { get; set; }
    }
}