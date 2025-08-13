using System.Text.Json.Serialization;

namespace QLN.Common.DTO_s.Implio
{
    public class ImplioImage
    {
        [JsonPropertyName("src")]
        public string Src { get; set; }
    }
}
