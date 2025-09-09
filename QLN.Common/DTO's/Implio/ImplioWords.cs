using System.Text.Json.Serialization;

namespace QLN.Common.DTO_s.Implio
{
    public class ImplioWords
    {
        [JsonPropertyName("word")]
        public string Word { get; set; }

        [JsonPropertyName("regex")]
        public string Regex { get; set; }
    }
}