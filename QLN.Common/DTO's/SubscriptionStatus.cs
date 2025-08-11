using System.Text.Json.Serialization;

namespace QLN.Common.Infrastructure.Subscriptions
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SubscriptionStatus
    {
        Active = 1,
        Failed = 0,
        PaymentPending = 2,
        Expired = 3,
        Cancelled = 4,
        OnHold = 5,
        Ready = 6,
        PendingActivation = 7,
        Deleted = 8,
        Suspended = 9
    }
}
