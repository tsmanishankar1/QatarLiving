using System.Text.Json.Serialization;

namespace QLN.Common.Infrastructure.Subscriptions
{
    public enum Vertical
    {
        [JsonPropertyName("Vehicles")]
        Vehicles = 1,
        [JsonPropertyName("Properties")]
        Properties = 2,
        [JsonPropertyName("Rewards")]
        Rewards = 11
    }
}
