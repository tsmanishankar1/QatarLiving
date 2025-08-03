namespace QLN.ContentBO.WebUI.Models
{
    public enum AdStatus
    {
        Draft = 0,
        PendingApproval = 1,
        Approved = 2,
        Published = 3,
        Unpublished = 4,
        Rejected = 5,
        Expired = 6,
        NeedsModification = 7
    }

    public enum BulkActionEnum
    {
        Approve = 1,
        Publish = 2,
        Unpublish = 3,
        UnPromote = 4,
        UnFeature = 5,
        Remove = 6,
        NeedChanges = 7
    }
    public enum VerifiedStatus
    {
        Pending = 1,
        Approved = 2,
        NeedChanges = 3,
        Rejected = 4
    }
}
