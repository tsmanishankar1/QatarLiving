using System.Text.Json.Serialization;

namespace QLN.Common.DTO_s.Implio
{
    public class ImplioOriginalAd
    {
        [JsonPropertyName("content")]
        public ImplioContent Content { get; set; }

        [JsonPropertyName("customerSpecific")]
        public Dictionary<string, string> CustomerSpecific { get; set; } = new Dictionary<string, string>();
    }

}
