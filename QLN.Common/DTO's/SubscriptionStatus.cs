using System.Text.Json.Serialization;

namespace QLN.Common.Infrastructure.Subscriptions
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SubscriptionStatus
    {
        PaymentPending = 1,
        PaymentFailed = 2,
        Active = 3,
        OnHold = 4,
        Expired = 5,
        Cancelled = 6
    }
}
