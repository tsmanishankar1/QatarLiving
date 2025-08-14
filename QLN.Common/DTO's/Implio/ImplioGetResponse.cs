using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.Implio
{
    public class ImplioGetResponse
    {
        [JsonPropertyName("pollingInfo")]
        public ImplioPollingInfo? PollingInfo { get; set; }

        [JsonPropertyName("ads")]
        public List<ImplioAd>? Ads { get; set; }
    }
}
