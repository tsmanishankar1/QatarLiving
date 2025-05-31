using System.Text.Json.Serialization;

namespace QLN.Common.Infrastructure.Subscriptions
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Vertical
    {
        [JsonPropertyName("Vehicles")]
        Vehicles = 0,
        [JsonPropertyName("Properties")]
        Properties = 1,
        [JsonPropertyName("Rewards")]
        Rewards = 2,
        [JsonPropertyName("Classifieds")]
        Classifieds = 3,
        [JsonPropertyName("Services")]
        Services = 4
    }
}
