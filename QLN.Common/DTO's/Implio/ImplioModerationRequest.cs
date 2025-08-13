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
        public ImplioCustomerSpecific? CustomerSpecific { get; set; } // not sure what can get populated into this field, so making it nullable

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
