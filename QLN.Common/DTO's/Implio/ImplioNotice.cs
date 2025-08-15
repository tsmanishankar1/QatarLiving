using System.Text.Json.Serialization;

namespace QLN.Common.DTO_s.Implio
{
    public class ImplioNotice
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("severity")]
        public string Severity { get; set; }
    }
}