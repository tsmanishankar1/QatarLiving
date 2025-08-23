using QLN.Common.DTO_s.Subscription;
using QLN.Common.Infrastructure.Subscriptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class UseMeResponseDto
    {
        public UserDetailsDto User { get; set; } = new();
        public UserSubscriptionSummaryDto Subscriptions { get; set; } = new();
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public string ApiVersion { get; set; } = "V2";
    }

    public class UserDetailsDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Gender { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string? LanguagePreferences { get; set; }
        public string? Nationality { get; set; }
        public string? MobileOperator { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<string> Roles { get; set; } = new();
        public List<UserCompanyDto> Companies { get; set; } = new();
        public UserLegacyDataDto? LegacyData { get; set; }
    }

    public class UserSubscriptionSummaryDto
    {
        public int TotalActiveSubscriptions { get; set; }
        public int TotalActiveP2PSubscriptions { get; set; }
        public int TotalActiveFreeSubscriptions { get; set; }
        public int TotalActiveAddons { get; set; }
        public Dictionary<Vertical, List<V2SubscriptionResponseDto>> ActiveSubscriptions { get; set; } = new();
        public Dictionary<Vertical, List<V2SubscriptionResponseDto>> ActiveP2PSubscriptions { get; set; } = new();
        public Dictionary<Vertical, List<V2SubscriptionResponseDto>> ActiveFreeSubscriptions { get; set; } = new();
        public Dictionary<Vertical, List<V2UserAddonResponseDto>> ActiveAddons { get; set; } = new();
        public OverallUsageSummaryDto UsageSummary { get; set; } = new();
        public DateTime? EarliestExpiryDate { get; set; }
        public DateTime? LatestExpiryDate { get; set; }
    }

    public class OverallUsageSummaryDto
    {
        public int TotalAdsRemaining { get; set; }
        public int TotalPromotionsRemaining { get; set; }
        public int TotalFeaturesRemaining { get; set; }
        public int TotalRefreshesRemaining { get; set; }
        public int TotalFreeAdsRemaining { get; set; }
        public bool HasActiveClassifieds { get; set; }
        public bool HasActiveProperties { get; set; }
        public bool HasActiveServices { get; set; }
        public bool HasActiveRewards { get; set; }
        public List<CategoryUsageSummaryDto> FreeAdsCategories { get; set; } = new();
    }

    public class CategoryUsageSummaryDto
    {
        public string Category { get; set; } = string.Empty;
        public string? L1Category { get; set; }
        public string? L2Category { get; set; }
        public string CategoryPath { get; set; } = string.Empty;
        public int AdsAllowed { get; set; }
        public int AdsUsed { get; set; }
        public int AdsRemaining { get; set; }
        public double UsagePercentage { get; set; }
    }

    public class UserCompanyDto
    {
        public Guid Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
    }

    public class UserLegacyDataDto
    {
        public long Uid { get; set; }
        public string Alias { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public bool IsAdmin { get; set; }
        public string? Language { get; set; }
        public List<string>? Roles { get; set; }
    }
}
