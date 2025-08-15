using System.Text.Json.Serialization;

namespace QLN.Common.DTO_s.Implio
{
    public class ImplioReason
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}
