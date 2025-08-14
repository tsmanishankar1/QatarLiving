using System.Text.Json.Serialization;

namespace QLN.Common.DTO_s.Implio
{
    public class ImplioUser
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("agent")]
        public string Agent { get; set; }

        [JsonPropertyName("phoneNumbers")]
        public List<string> PhoneNumbers { get; set; }

        [JsonPropertyName("emailAddresses")]
        public List<string> EmailAddresses { get; set; }

        [JsonPropertyName("customerSpecific")]
        public Dictionary<string, string> CustomerSpecific { get; set; } = new Dictionary<string, string>();
    }
}
