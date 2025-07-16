
using QLN.Common.Infrastructure.Subscriptions;


namespace QLN.Common.DTO_s
{
    public class V2BannerDto
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
        public string Createdby { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Updatedby { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
    public class V2BannerTypeDto
    {
        public Guid Id { get; set; }
        public Vertical VerticalId { get; set; }
        public SubVertical SubVerticalId { get; set; }
        public Guid PageId { get; set; }
        public string Dimensions { get; set; }
        public string BannerslotId { get; set; }
      
        public List<Guid> BannerTypeIds { get; set; } = new(); 
    }

    public class V2BannerLocationDto
    {
        public Guid Id { get; set; }
        public string BannerTypeName { get; set; }
    }
    public class V2BannerPageLocationDto
    {
        public Guid Id { get; set; }
        public string BannerPageName { get; set; }
    }
    public class BannerTypeDetailsDto
    {
        public string VerticalName { get; set; }
        public string SubVerticalName { get; set; }
        public Guid PageId { get; set; }
        public string PageName { get; set; }
        public string Dimensions { get; set; }
        public string BannerslotId { get; set; }


        public List<BannerTypeItemDto> BannerTypes { get; set; }
    }
    public class V2BannerTypeDetailedDto
    {
        public Guid Id { get; set; }

        public int VerticalId { get; set; }
        public string VerticalName { get; set; }

        public int SubVerticalId { get; set; }
        public string SubVerticalName { get; set; }

        public List<Guid> BannerTypeIds { get; set; } = new();
        public List<string> BannerTypeNames { get; set; } = new();

        public Guid PageId { get; set; }
        public string PageName { get; set; }
    }


    public class BannerTypeItemDto
    {
        public Guid BannerTypeId { get; set; }
        public string BannerTypeName { get; set; }
    }
    public class DeleteBannerRequest
    {
        public Guid BannerId { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
    }


}
