using System.Text.Json.Serialization;

namespace QLN.Common.Infrastructure.Subscriptions
{
    public enum SubscriptionName
    {
        [JsonPropertyName("1 week Subscription")]
        AlayaOneWeek,
        [JsonPropertyName("1 month Subscription")]
        AlayaOneMonth,
        [JsonPropertyName("3 months Subscriptions")]
        AlayaThreeMonth,
        [JsonPropertyName("Regular Package")]
        Regular
    }
}
