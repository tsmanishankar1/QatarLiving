using System.ComponentModel.DataAnnotations;
namespace QLN.ContentBO.WebUI.Models
{
    public class ServicesDto
    {
        public Guid Id { get; set; }
        [Required]
        public Guid CategoryId { get; set; }
        [Required]
        public Guid L1CategoryId { get; set; }
        [Required]
        public Guid L2CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? L1CategoryName { get; set; }
        public string? L2CategoryName { get; set; }
        public bool IsPriceOnRequest { get; set; }
        public decimal? Price { get; set; }
        [Required]
        public string Title { get; set; }
        [Required]
        public string Description { get; set; }
        [Required]
        public string PhoneNumberCountryCode { get; set; }
        [Required]
        public string PhoneNumber { get; set; }
        [Required]
        public string WhatsappNumberCountryCode { get; set; }
        [Required]
        public string WhatsappNumber { get; set; }
        [EmailAddress]
        public string? EmailAddress { get; set; }
        [Required]
        public string Location { get; set; }
        public int? LocationId { get; set; }
        public decimal Longitude { get; set; }
        public decimal Lattitude { get; set; }
        public List<ImageDto>? PhotoUpload { get; set; }
        public string? UserName { get; set; }
        public ServiceStatus? Status { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsPromoted { get; set; }
        public bool IsRefreshed { get; set; }
        public DateTime? PromotedExpiryDate { get; set; }
        public DateTime? FeaturedExpiryDate { get; set; }
        public DateTime? RefreshExpiryDate { get; set; }
        public ServiceAdType AdType { get; set; }
        public DateTime? PublishedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
    public class ImageDto
    {
        public string? FileName { get; set; }
        public string? Url { get; set; }
        public int Order { get; set; }
    }
    public enum ServiceStatus
    {

        Draft = 1,
        PendingApproval = 2,
        Published = 3,
        Unpublished = 4,
        Rejected = 5
    }
    public enum ServiceAdType
    {
        PayToPublish = 1,
        Subscription = 2
    }
}