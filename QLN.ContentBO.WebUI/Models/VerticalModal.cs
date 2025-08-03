namespace QLN.ContentBO.WebUI.Models
{
    public enum VerticalTypeEnum
    {
        Jobs = 1,
        Properties = 2,
        Classifieds = 3,
        Services = 4
    }
    public enum SubVerticalTypeEnum
    {
        Items = 1,
        Deals = 2,
        Stores = 3,
        Preloved = 4,
        Collectibles = 5,
        Services = 6,
        News = 7,
        Daily = 8,
        Events = 9,
        Community = 10
    }

    public enum CompanyStatus
    {
        Active = 1,
        Blocked = 2,
        Suspended = 3,
        Unblocked = 4,
        PendingLicenseApproval = 5,
        NeedChanges = 6,
        Rejected = 7
    }
}
