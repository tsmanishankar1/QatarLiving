using System.Text.Json.Serialization;

namespace QLN.Common.DTO_s.Implio
{
    public class ImplioAd
    {
        [JsonPropertyName("items")]
        public List<ImplioItem> Items { get; set; }

        [JsonPropertyName("packedAt")]
        public long PackedAt { get; set; }

        [JsonPropertyName("result")]
        public ImplioResult Result { get; set; }
    }
}
