using Microsoft.EntityFrameworkCore;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.Service.TimeSpanConverter;
using QLN.Common.Infrastructure.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
namespace QLN.Common.DTO_s.ClassifiedsBo
{
    public class ClassifiedBOPageResponse<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public long TotalCount { get; set; }
        public int? Page { get; set; }
        public int? PerPage { get; set; }
    }
    
    public class StoresSubscriptionDto
    {
        [Key]
        public int OrderId { get; set; }
        public string? SubscriptionType { get; set; } = null;
        public string? UserName { get; set; } = null;
        public string? Email { get; set; } = null;
        public string? Mobile { get; set; } = null;
        public string? Whatsapp { get; set; } = null;
        public string? WebUrl { get; set; } = null;
        public decimal Amount { get; set; } = 0;
        public string? Status { get; set; } = null;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int WebLeads { get; set; } = 0;
        public int EmailLeads { get; set; } = 0;
        public int WhatsappLeads { get; set; } = 0;
        public int PhoneLeads { get; set; } = 0;

    }

    public class ViewStoresSubscription
    {
        public Guid CompanyId { get; set; }
        public Guid SubscriptionId { get; set; }
        public string? SubscriptionType { get; set; } = null;
        public string? UserId { get; set; } = null;
        public string? UserName { get; set; } = null;
        public string? CompanyName { get; set; } = null;
        public string? Mobile { get; set; } = null;
        public string? Whatsapp { get; set; } = null;
        public string? WebUrl { get; set; } = null;
        public string? Email { get; set; } = null;
        public int Status { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int OrderId { get; set; }
        public decimal Amount { get; set; } = 0;
        

    }
    public class ViewStoresSubscriptionDto
    {
        public string? CompanyId { get; set; }
        public string? SubscriptionId { get; set; }
        public string? SubscriptionType { get; set; } = null;
        public string? UserId { get; set; } = null;
        public string? UserName { get; set; } = null;
        public string? CompanyName { get; set; } = null;
        public string? Mobile { get; set; } = null;
        public string? Whatsapp { get; set; } = null;
        public string? WebUrl { get; set; } = null;
        public string? Email { get; set; } = null;
        public string? Status { get; set; } = null;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int OrderId { get; set; }
        public decimal Amount { get; set; } = 0;
        public int WebLeads { get; set; } = 0;
        public int EmailLeads { get; set; } = 0;
        public int WhatsappLeads { get; set; } = 0;
        public int PhoneLeads { get; set; } = 0;

    }
    public class ClassifiedStoreResponse
    {
        public List<StoresGroup> Stores { get; set; } = new();
    }
    public class ClassifiedStoresProducts
    {
        public List<ClassifiedStoresIndex> Products { get; set; } = new();
    }

    public class StoresGroup
    {
        public Guid CompanyId { get; set; }
        public Guid SubscriptionId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string? ContactNumber { get; set; }
        public string? Email { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string BannerUrl { get; set; } = string.Empty;
        public string WebsiteUrl { get; set; } = string.Empty;
        public List<string> Locations { get; set; } = new();
        public int ProductCount { get; set; }
        public string? StoreSlug { get; set; }
        public List<ProductInfo> Products { get; set; } = new();
    }

    public class ProductInfo
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductSlug { get; set; } = string.Empty;
        public string ProductLogo { get; set; } = string.Empty;
        public double ProductPrice { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string ProductSummary { get; set; } = string.Empty;
        public string ProductDescription { get; set; } = string.Empty;
        public List<string> Features { get; set; } = new();
        public List<string> Images { get; set; } = new();
       
    }

    public class StoreCompanyDto
    {
        public Guid Id { get; set; }
        public string CompanyName { get; set; } 
        public string CompanyLogo { get; set; }
        public string? CoverImage1 { get; set; }
        public string PhoneNumber { get; set; } 
        public string Email { get; set; } 
        public string? WebsiteUrl { get; set; }
        public List<string>? BranchLocations { get; set; }
        public string? Slug { get; set; }

    }

    public class StoreSubscriptionQuotaDto
    {
        public Guid SubscriptionId { get; set; }
        public string QuotaJson { get; set; } 
      
    }

    public class StoreSubscriptionQuota
    {
        public Guid SubscriptionId { get; set; }
        public string Quota { get; set; }
        public int TotalInventory { get; set; }
        public int Inventory { get; set; }

    }

    public class SubscriptionToken
    {
        public Guid UserId { get; set; }
        public Guid Id { get; set; }
        public string DisplayName { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public int? Vertical { get; set; }
        public int? SubVertical { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class QuotaUsageSummary
    {
        public string Scope { get; set; }
        public int AdsUsed { get; set; }
        public string Vertical { get; set; }
        public string? SocialMedia { get; set; }
        public int FeaturesUsed { get; set; }
        public bool CanFeatureAds { get; set; }
        public bool CanPromoteAds { get; set; }
        public bool CanPublishAds { get; set; }
        public bool CanRefreshAds { get; set; }
        public DateTime LastDailyReset { get; set; }
        public int PromotionsUsed { get; set; }
        public bool CanUnFeatureAds { get; set; }
        public bool CanUnPromoteAds { get; set; }
        public bool CanUnPublishAds { get; set; }
        public string LastRefreshUsed { get; set; } 
        public DateTime LastUsageUpdate { get; set; }
        public string RefreshInterval { get; set; }
        public int TotalAdsAllowed { get; set; }
        public bool CanPostSocialMedia { get; set; }
        public int DailyRefreshesUsed { get; set; }
        public int RefreshesPerAdUsed { get; set; }
        public int RefreshIntervalHours { get; set; }
        public int SocialMediaPostsUsed { get; set; }
        public int TotalFeaturesAllowed { get; set; }
        public int DailyRefreshesAllowed { get; set; }
        public int? ListingsPerL2Category { get; set; }
        public int RefreshesPerAdAllowed { get; set; }
        public int TotalPromotionsAllowed { get; set; }
        public int SocialMediaPostsAllowed { get; set; }
    }


}
