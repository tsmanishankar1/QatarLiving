using System.Text.Json.Serialization;

namespace QLN.Common.DTO_s.Implio
{
    public class ImplioParams
    {
        [JsonPropertyName("key")]
        public string Key { get; set; }
    }

}
