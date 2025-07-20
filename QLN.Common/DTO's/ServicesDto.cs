using QLN.Common.Infrastructure.CustomEndpoints.PayToPublishEndpoint;
using System.ComponentModel.DataAnnotations;

namespace QLN.Common.DTO_s
{
    public class ServicesDto
    {
        public Guid Id { get; set; }
        public Guid CategoryId { get; set; }
        public Guid L1CategoryId { get; set; }
        public Guid L2CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? L1CategoryName { get; set; }
        public string? L2CategoryName { get; set; }
        public double? Price { get; set; }
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
        public string Location { get; set; }
        public int? LocationId { get; set; }
        public string Longitude { get; set; }
        public string Latitude { get; set; }
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
    public class DeleteServiceRequest
    {
        public Guid Id { get; set; }
        public string? UpdatedBy { get; set; }
    }
    public enum ServiceStatus
    {
        PendingApproval = 1,
        Published = 2,
        Unpublished = 3,
        Rejected = 4
    }
    public enum ServiceAdType
    {
        PayToPublish = 1,
        Subscription = 2
    }
    public class PromoteServiceRequest
    {
        public Guid ServiceId { get; set; }
        public bool IsPromoted { get; set; } 
    }
    public class FeatureServiceRequest
    {
        public Guid ServiceId { get; set; }
        public bool IsFeature { get; set; }
    }
    public class ServicesPagedResponse<T>
    {
        public int TotalCount { get; set; }
        public int? PageNumber { get; set; }
        public int? PerPage { get; set; }
        public List<T> Items { get; set; } = new();
    }
    public class ServiceStatusQuery
    {
        public ServiceStatus? Status { get; set; }
        public int? PageNumber { get; set; }
        public int? PerPage { get; set; } 
    }
}
