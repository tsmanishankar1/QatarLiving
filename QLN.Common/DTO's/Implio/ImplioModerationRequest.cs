using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.Implio
{

    public class ImplioModerationRequest
    {
        [JsonPropertyName("id")]
        public required string Id { get; set; }

        [JsonPropertyName("customerSpecific")]
        public Dictionary<string, string> CustomerSpecific { get; set; } = new Dictionary<string, string>();

        [JsonPropertyName("content")]
        public ImplioContent Content { get; set; }

        [JsonPropertyName("user")]
        public ImplioUser User { get; set; }

        [JsonPropertyName("location")]
        public ImplioLocation Location { get; set; }

        [JsonPropertyName("result")]
        public ImplioResult? Result { get; set; }
    }
}
