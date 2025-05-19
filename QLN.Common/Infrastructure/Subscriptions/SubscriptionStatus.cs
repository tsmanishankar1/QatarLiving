namespace QLN.Common.Infrastructure.Subscriptions
{
    public enum SubscriptionStatus
    {
        Active = 1,
        Failed = 0,
        Pending = 2,
        Expired = 3,
        Cancelled = 4,
        OnHold = 5,
        Ready = 6,
        PendingActivation = 7,
        Deleted = 8
    }
}
