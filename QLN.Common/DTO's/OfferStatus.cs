namespace QLN.Common.Infrastructure.Subscriptions
{
    public enum OfferStatus
    {
        Draft = -1,
        Unpublished = 0,
        Published = 1,
        Sold = 2,
        PaymentPending = 3,
        Remove = 4,
        PendingApproval = 5,
        Approved = 6,
        Rejected = 7,
        NeedChanges = 8,
        Expired = 9,
        Featured = 10
    }
}
