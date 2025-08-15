using System.Text.Json.Serialization;

namespace QLN.Common.DTO_s.Implio
{
    public class ImplioType
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}
