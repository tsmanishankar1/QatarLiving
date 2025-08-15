using QLN.Common.Infrastructure.Subscriptions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.Subscription
{
    #region Core DTOs (Internal Use - Actor/Service)

    /// <summary>
    /// V2 Subscription DTO - Maps to Subscription table
    /// </summary>
    public class V2SubscriptionDto
    {
        public Guid Id { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public Guid? CompanyId { get; set; }
        public int? PaymentId { get; set; }
        public Vertical Vertical { get; set; }
        public SubVertical? SubVertical { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; } = "QAR";
        public SubscriptionQuota Quota { get; set; } = new();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public SubscriptionStatus StatusId { get; set; }
        public DateTime lastUpdated { get; set; }
        public string Version { get; set; } = "V2";
    }

    /// <summary>
    /// V2 User Addon DTO - Maps to UserAddOn table
    /// </summary>
    public class V2UserAddonDto
    {
        public Guid Id { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public Guid? CompanyId { get; set; }
        public Guid SubscriptionId { get; set; }
        public int? PaymentId { get; set; }
        public Vertical Vertical { get; set; }
        public SubVertical? SubVertical { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; } = "QAR";
        public SubscriptionQuota Quota { get; set; } = new();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public V2Status StatusId { get; set; }
        public DateTime lastUpdated { get; set; }
        public string Version { get; set; } = "V2";
    }

    #endregion

    #region Status Enum

    /// <summary>
    /// V2 Status enum - Maps to SubscriptionStatus
    /// </summary>
    public enum V2Status
    {
        PaymentPending = 1,
        Active = 2,
        Expired = 3,
        Cancelled = 4,
        Suspended = 5
    }

    #endregion

    #region Request DTOs (API Input)

    /// <summary>
    /// Request to purchase a subscription product
    /// </summary>
    public class V2SubscriptionPurchaseRequestDto
    {
        [Required]
        [StringLength(50, ErrorMessage = "Product code must not exceed 50 characters")]
        public string ProductCode { get; set; } = string.Empty;

        [Required]
        [StringLength(100, ErrorMessage = "User ID must not exceed 100 characters")]

        public Guid? CompanyId { get; set; }

        public int? PaymentId { get; set; }
        public string? UserId { get; set; } = string.Empty;
        public long? AdId { get; set; }

    }

    /// <summary>
    /// Request to purchase an addon product
    /// </summary>
    public class V2UserAddonPurchaseRequestDto
    {
        [Required]
        [StringLength(50, ErrorMessage = "Product code must not exceed 50 characters")]
        public string ProductCode { get; set; } = string.Empty;

        [Required]
        [StringLength(100, ErrorMessage = "User ID must not exceed 100 characters")]


        public Guid? CompanyId { get; set; }

        [Required]
        public Guid SubscriptionId { get; set; }

        public int? PaymentId { get; set; }
        public string? UserId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request to validate quota usage
    /// </summary>
    public class V2UsageValidationRequest
    {
        [Required]
        public Guid SubscriptionId { get; set; }

        [Required]
        [StringLength(50, ErrorMessage = "Quota type must not exceed 50 characters")]
        public string QuotaType { get; set; } = string.Empty;


        [Required]
        [Range(1, 999999, ErrorMessage = "Requested amount must be between 0.01 and 999999.99")]
        public int RequestedAmount { get; set; }
    }

    /// <summary>
    /// Request to record quota usage
    /// </summary>
    public class V2UsageRecordRequest
    {
        [Required]
        public Guid SubscriptionId { get; set; }

        [Required]
        [StringLength(50, ErrorMessage = "Quota type must not exceed 50 characters")]
        public string QuotaType { get; set; } = string.Empty;


        [Required]
        [Range(1, 999999, ErrorMessage = "Requested amount must be between 1 and 999,999")]
        public int Amount { get; set; }
    }

    /// <summary>
    /// Request for addon usage operations
    /// </summary>
    public class V2AddonUsageRequest
    {
        [Required]
        public Guid AddonId { get; set; }

        [Required]
        [StringLength(50, ErrorMessage = "Quota type must not exceed 50 characters")]
        public string QuotaType { get; set; } = string.Empty;

        [Required]
        [Range(1, 999999, ErrorMessage = "Requested amount must be between 1 and 999,999")]
        public int RequestedAmount { get; set; }

    }

    #endregion

    #region Response DTOs (API Output)

    /// <summary>
    /// Enhanced subscription response with calculated fields
    /// </summary>
    public class V2SubscriptionResponseDto
    {
        public Guid Id { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public string VerticalName { get; set; } = string.Empty;
        public Vertical Vertical { get; set; }
        public SubVertical? SubVertical { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; } = string.Empty;
        public SubscriptionQuota Quota { get; set; } = new();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public SubscriptionStatus StatusId { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int DaysRemaining { get; set; }
        public string Version { get; set; } = "V2";
    }

    /// <summary>
    /// Enhanced addon response with calculated fields
    /// </summary>
    public class V2UserAddonResponseDto
    {
        public Guid Id { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public Guid SubscriptionId { get; set; }
        public string VerticalName { get; set; } = string.Empty;
        public Vertical Vertical { get; set; }
        public SubVertical? SubVertical { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; } = string.Empty;
        public SubscriptionQuota Quota { get; set; } = new();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public V2Status StatusId { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int DaysRemaining { get; set; }
        public string Version { get; set; } = "V2";
    }

    /// <summary>
    /// Grouped subscriptions by vertical
    /// </summary>
    public class V2SubscriptionGroupResponseDto
    {
        public int VerticalTypeId { get; set; }
        public string VerticalName { get; set; } = string.Empty;
        public List<V2SubscriptionResponseDto> Subscriptions { get; set; } = new();
        public int TotalCount { get; set; }
        public string Version { get; set; } = "V2";
    }

    /// <summary>
    /// Purchase confirmation response
    /// </summary>
    public class V2PurchaseResponseDto
    {
        public Guid Id { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool Success { get; set; }
        public DateTime PurchasedAt { get; set; }
        public DateTime ExpiresAt { get; set; }

    }

    /// <summary>
    /// Usage validation response
    /// </summary>
    public class V2UsageValidationResponseDto
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public Guid SubscriptionId { get; set; }
        public string QuotaType { get; set; } = string.Empty;
        public int RequestedAmount { get; set; }
        public decimal AvailableQuota { get; set; }
        public string Version { get; set; } = "V2";

    }

    /// <summary>
    /// Usage recording response
    /// </summary>
    public class V2UsageRecordResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Guid SubscriptionId { get; set; }
        public string QuotaType { get; set; } = string.Empty;
        public int AmountRecorded { get; set; }
        public decimal RemainingQuota { get; set; }
        public DateTime RecordedAt { get; set; }
        public string Version { get; set; } = "V2";

    }

    #endregion

    #region Event DTOs (Pub/Sub)

    /// <summary>
    /// Subscription expiration event for pub/sub
    /// </summary>
    public class V2SubscriptionExpiredEventDto
    {
        public Guid SubscriptionId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public Vertical Vertical { get; set; }
        public SubVertical? SubVertical { get; set; }
        public DateTime ExpiredAt { get; set; }
        public Guid EventId { get; set; }
        public string Version { get; set; } = "V2";
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Addon expiration event for pub/sub
    /// </summary>
    public class V2AddonExpiredEventDto
    {
        public Guid AddonId { get; set; }
        public Guid SubscriptionId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public Vertical Vertical { get; set; }
        public SubVertical? SubVertical { get; set; }
        public DateTime ExpiredAt { get; set; }
        public Guid EventId { get; set; }
        public string Version { get; set; } = "V2";
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    #endregion

    #region Filter/Query DTOs (Optional - for advanced queries)

    /// <summary>
    /// Filter for subscription queries
    /// </summary>
    public class V2SubscriptionFilterDto
    {
        public string? UserId { get; set; }
        public Guid? CompanyId { get; set; }
        public Vertical Vertical { get; set; }
        public SubVertical? SubVertical { get; set; }
        public V2Status? StatusId { get; set; }
        public DateTime? StartDateFrom { get; set; }
        public DateTime? StartDateTo { get; set; }
        public DateTime? EndDateFrom { get; set; }
        public DateTime? EndDateTo { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsExpired { get; set; }

        [Range(1, 1000, ErrorMessage = "Page must be between 1 and 1000")]
        public int Page { get; set; } = 1;

        [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// Paginated response wrapper
    /// </summary>
    public class V2PaginatedResponseDto<T>
    {
        public List<T> Data { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
        public string Version { get; set; } = "V2";
    }

    #endregion

    #region Product Constraints DTO (Helper)

    /// <summary>
    /// Product constraints structure for quota extraction
    /// </summary>
    public class V2ProductConstraintsDto
    {
        public decimal? AdsBudget { get; set; }
        public decimal? PromoteBudget { get; set; }
        public decimal? RefreshBudget { get; set; }
        public decimal? FeatureBudget { get; set; }
        public TimeSpan? Duration { get; set; }
        public int? MaxListings { get; set; }
        public bool? AutoRenewal { get; set; }
        public int? MaxRefreshPerDay { get; set; }
        public bool? AllowPromote { get; set; }
        public bool? AllowFeature { get; set; }
        public Dictionary<string, object> AdditionalConstraints { get; set; } = new();
    }

    #endregion

    #region Common Quota Types (Constants)

    /// <summary>
    /// Common quota type constants
    /// </summary>
    public static class V2QuotaTypes
    {
        public const string AdsBudget = "ads_budget";
        public const string PromoteBudget = "promote_budget";
        public const string RefreshBudget = "refresh_budget";
        public const string FeatureBudget = "feature_budget";
        public const string MaxListings = "max_listings";
        public const string SocialMediaPosts = "social_media_posts";
        public const string BannerImpressions = "banner_impressions";
    }

    #endregion
}
