namespace QLN.ContentBO.WebUI.Models
{
    public class VerificationProfileStatus : EventDTO
    {
        public Guid? CompanyId { get; set; }
        public string? UserId { get; set; }
        public string? CRFile { get; set; }
        public int? CRLicense { get; set; }
        public DateOnly Enddate { get; set; }
        public string? Username { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public VerticalType Vertical { get; set; }
        public SubVerticalType SubVertical { get; set; }
        public bool IsActive { get; set; }

    }
    public enum VerticalType
    {
        Jobs = 1,
        Properties = 2,
        Classifieds = 3,
        Services = 4
    }
    public enum SubVerticalType
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

 
}