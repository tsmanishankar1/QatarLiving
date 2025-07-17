namespace QLN.ContentBO.WebUI.Models
{
    public class Banner
    {
        public Guid Id { get; set; }
        public bool Status { get; set; }
        public Guid BannerTypeId { get; set; }
        public string AnalyticsTrackingId { get; set; }
        public string AltText { get; set; }
        public string LinkUrl { get; set; }
        public int Duration { get; set; }
        public string BannerSize { get; set; }
        public bool? IsDesktopAvailability { get; set; }
        public bool? IsMobileAvailability { get; set; }
        public string? DesktopImage { get; set; }
        public string? MobileImage { get; set; }
    }
}
