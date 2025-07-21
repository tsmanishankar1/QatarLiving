using System.ComponentModel.DataAnnotations;
namespace QLN.ContentBO.WebUI.Models
{
    public class BannerDTO
    {
        public Guid Id { get; set; }
        public bool Status { get; set; } = true;
        public int? slotId { get; set; }
        [Required(ErrorMessage = "Banner location is required.")]
        public List<BannerTypeRequest> BannerTypeIds { get; set; } = new();
        public string? BannerTypeId { get; set; } = "";
        [Required(ErrorMessage = "Analytics Tracking ID is required.")]
        public string AnalyticsTrackingId { get; set; }
        [Required(ErrorMessage = "Alt Text is required.")]
        public string AltText { get; set; }
        [Required(ErrorMessage = "Link URL is required.")]
        [Url(ErrorMessage = "Invalid URL format.")]
        public string LinkUrl { get; set; }
        public int Duration { get; set; } = 5;
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        [Required(ErrorMessage = "Banner size is required.")]
        public string BannerSize { get; set; }
        public bool? IsDesktopAvailability { get; set; } = false;
        public bool? IsMobileAvailability { get; set; } = false;
        public string? DesktopImage { get; set; }
        public string? MobileImage { get; set; }
        public string Createdby { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Updatedby { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
    public class BannerTypeRequest
    {
        public Guid BannerTypeId { get; set; }
        public Vertical VerticalId { get; set; }
        public SubVertical? SubVerticalId { get; set; }
        public Guid PageId { get; set; }
    }

    public class BannerLocationDto
    {
        public Guid Id { get; set; }
        public string BannerTypeName { get; set; }
        public string Dimensions { get; set; }
        public string BannerslotId { get; set; }
        public List<BannerDTO>? BannerDetails { get; set; } = [];
    }

    public class BannerPageLocationDto
    {
        public Guid Id { get; set; }
        public int? VerticalId { get; set; }
       public int? SubVerticalId { get; set; }
        public string BannerPageName { get; set; }
        public List<BannerLocationDto> bannertypes { get; set; }
    }
    public enum Vertical
    {

        Vehicles = 0,
        Properties = 1,
        Rewards = 2,
        Classifieds = 3,
        Services = 4,
        Content = 5
    }
    public enum SubVertical
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
