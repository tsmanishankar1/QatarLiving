using QLN.Common.Infrastructure.Subscriptions;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QLN.Common.DTO_s
{
    public class ServiceDto
    {
        [Required]
        public long CategoryId { get; set; }
        [Required]
        public long L1CategoryId { get; set; }
        [Required]
        public long L2CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? L1CategoryName { get; set; }
        public string? L2CategoryName { get; set; }
        public bool IsPriceOnRequest { get; set; }
        public decimal? Price { get; set; }
        [Required]
        public string Title { get; set; }
        [Required]
        public string Description { get; set; } = string.Empty;
        [Required]
        public string PhoneNumberCountryCode { get; set; } = string.Empty;
        [Required]
        public string PhoneNumber { get; set; } = string.Empty;
        [Required]
        public string WhatsappNumberCountryCode { get; set; } = string.Empty;
        [Required]
        public string WhatsappNumber { get; set; } = string.Empty;
        [EmailAddress]
        public string? EmailAddress { get; set; }
        [Required]
        public string Location { get; set; } = string.Empty;
        public int? LocationId { get; set; }
        [Required]
        public string ZoneId { get; set; } = string.Empty;
        public string? StreetNumber { get; set; }
        public string? BuildingNumber { get; set; }
        public string? LicenseCertificate { get; set; }
        public string? Comments { get; set; }
        public decimal Longitude { get; set; }
        public decimal Lattitude { get; set; }
        public List<ImageDto>? PhotoUpload { get; set; }
        public ServiceStatus? Status { get; set; }
        public ServiceAdType AdType { get; set; }
        public string? Availability { get; set; }
        public string? Duration { get; set; }
        public string? Reservation { get; set; }
    }
    public class ServiceRequest : ServiceDto
    {
        public string CreatedBy { get; set; }
        public string userName { get; set; }
    }
    public class ImageDto
    {
        public string? Url { get; set; }
        public int Order { get; set; }
    }
    public class DeleteServiceRequest
    {
        public long Id { get; set; }
        public string? UpdatedBy { get; set; }
    }
    public enum ServiceStatus
    {
        Draft = 0,
        PendingApproval = 1,
        Approved = 2,
        Published = 3,
        Unpublished = 4,
        Rejected = 5,
        Expired = 6,
        NeedsModification = 7,
        Hold = 8,
        Onhold = 9
    }
    public enum BulkModerationAction
    {
        Approve = 1,
        Publish = 2,
        Unpublish = 3,
        UnPromote = 4,
        UnFeature = 5,
        Remove = 6,
        NeedChanges = 7,
        Promote = 8,
        Feature = 9,
        Hold = 10,
        Onhold = 11,
        IsRefreshed = 12
    }
    public enum ServiceAdType
    {
        PayToPublish = 1,
        Subscription = 2
    }
    public class PromoteServiceRequest
    {
        public long ServiceId { get; set; }
        public bool IsPromoted { get; set; }
        [JsonIgnore]
        public string? UpdatedBy { get; set; } = null!;
    }
    public class FeatureServiceRequest
    {
        public long ServiceId { get; set; }
        public bool IsFeature { get; set; }
        [JsonIgnore]
        public string? UpdatedBy { get; set; } = null!;
    }
    public class RefreshServiceRequest
    {
        public long ServiceId { get; set; }
        public bool IsRefreshed { get; set; }
        [JsonIgnore]
        public string? UpdatedBy { get; set; } = null!;
    }
    public class PublishServiceRequest
    {
        public long ServiceId { get; set; }
        public ServiceStatus? Status { get; set; }
        [JsonIgnore]
        public string? UpdatedBy { get; set; } = null!;
    }
    public class ServicesPagedResponse<T>
    {
        public int TotalCount { get; set; }
        public int PublishedCount { get; set; }
        public int UnpublishedCount { get; set; }
        public int? PageNumber { get; set; }
        public int? PerPage { get; set; }
        public List<T> Items { get; set; } = new();
    }
    public class AllServices
    {
        public long? TotalCount { get; set; }
        public List<ServicesIndex>? ServicesItems { get; set; }
    }
    public class BulkModerationRequest
    {
        public List<long> AdIds { get; set; } = new();
        public BulkModerationAction Action { get; set; }
        public string? Reason { get; set; }
        [JsonIgnore]
        public string? UpdatedBy { get; set; } 
    }
    public class BasePaginationQuery
    {
        public string? Title { get; set; }
        public string? SortBy { get; set; }
        public int? PageNumber { get; set; }
        public int? PerPage { get; set; }
        public Dictionary<string, JsonElement>? Filters { get; set; }
    }
    public class SubscriptionBudgetDto
    {
        // Totals
        public int TotalAdsAllowed { get; set; }
        public int TotalPromotionsAllowed { get; set; }
        public int TotalFeaturesAllowed { get; set; }
        public int DailyRefreshesAllowed { get; set; }
        public int RefreshesPerAdAllowed { get; set; }
        public int SocialMediaPostsAllowed { get; set; }

        // Used
        public int AdsUsed { get; set; }
        public int PromotionsUsed { get; set; }
        public int FeaturesUsed { get; set; }
        public int DailyRefreshesUsed { get; set; }
        public int RefreshesPerAdUsed { get; set; }
        public int SocialMediaPostsUsed { get; set; }
    }

    public class SubscriptionIdRequest
    {
        public Guid SubscriptionId { get; set; }
    }
    public class SubscriptionRequest
    {
        public Guid SubscriptionId { get; set; }
        public Vertical VerticalId { get; set; }
        public SubVertical? SubVerticalId { get; set; }
    }
    public class CategoryAdCountDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int Count { get; set; }
    }
}
