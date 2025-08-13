using System.Text.Json.Serialization;

namespace QLN.Common.DTO_s.Implio
{
    public class ImplioWordHighlighting
    {
        [JsonPropertyName("variableName")]
        public string VariableName { get; set; }

        [JsonPropertyName("words")]
        public List<ImplioWords> Words { get; set; }
    }
}