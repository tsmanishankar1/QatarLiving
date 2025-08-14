using System.Text.Json.Serialization;

namespace QLN.Common.DTO_s.Implio
{
    public class ImplioPollingInfo
    {
        [JsonPropertyName("newTimestamp")]
        public long NewTimestamp { get; set; }

        [JsonPropertyName("newerAdsExist")]
        public bool NewerAdsExist { get; set; }
    }
}
