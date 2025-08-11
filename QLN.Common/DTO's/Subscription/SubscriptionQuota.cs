using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.Subscription
{
    public static class ActionTypes
    {
        public const string Publish = "publish";
        public const string Promote = "promote";
        public const string Feature = "feature";
        public const string Refresh = "refresh";
        public const string SocialMediaPost = "social_media_post";
    }

    public sealed class ValidationResult
    {
        public string ActionType { get; set; } = string.Empty;
        public int RequestedQuantity { get; set; }
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public int RemainingQuota { get; set; }
    }

    public sealed class SocialMediaQuota
    {
        public int PostsAllowed { get; set; } = 0;
        public int PostsUsed { get; set; } = 0;
    }

    public sealed class SubscriptionQuota
    {
        // Totals
        public int TotalAdsAllowed { get; set; } = 0;
        public int TotalPromotionsAllowed { get; set; } = 0;
        public int TotalFeaturesAllowed { get; set; } = 0;
        public int DailyRefreshesAllowed { get; set; } = 0;
        public int RefreshesPerAdAllowed { get; set; } = 1;
        public int SocialMediaPostsAllowed { get; set; } = 0;

        // Used
        public int AdsUsed { get; set; } = 0;
        public int PromotionsUsed { get; set; } = 0;
        public int FeaturesUsed { get; set; } = 0;
        public int DailyRefreshesUsed { get; set; } = 0;
        public int RefreshesPerAdUsed { get; set; } = 0;
        public int SocialMediaPostsUsed { get; set; } = 0;

        // Flags
        public bool CanPublishAds { get; set; } = true;
        public bool CanPromoteAds { get; set; } = true;
        public bool CanFeatureAds { get; set; } = true;
        public bool CanRefreshAds { get; set; } = true;
        public bool CanPostSocialMedia { get; set; } = false;

        // Daily tracking
        public DateTime LastDailyReset { get; set; } = DateTime.UtcNow.Date;
        public DateTime LastRefreshUsed { get; set; } = DateTime.MinValue;
        public DateTime LastUsageUpdate { get; set; } = DateTime.UtcNow;

        // Refresh constraints
        public string RefreshInterval { get; set; } = "Every 72 Hours";
        public int RefreshIntervalHours { get; set; } = 72;

        // Extra metadata
        public string Vertical { get; set; } = string.Empty;
        public string Scope { get; set; } = "All";
        public int? ListingsPerL2Category { get; set; }
        public SocialMediaQuota? SocialMedia { get; set; }

        // Derived
        public int RemainingAds => Math.Max(0, TotalAdsAllowed - AdsUsed);
        public int RemainingPromotions => Math.Max(0, TotalPromotionsAllowed - PromotionsUsed);
        public int RemainingFeatures => Math.Max(0, TotalFeaturesAllowed - FeaturesUsed);
        public int RemainingDailyRefreshes => Math.Max(0, DailyRefreshesAllowed - DailyRefreshesUsed);
        public int RemainingRefreshesPerAd => Math.Max(0, RefreshesPerAdAllowed - RefreshesPerAdUsed);
        public int RemainingSocialMediaPosts => Math.Max(0, SocialMediaPostsAllowed - SocialMediaPostsUsed);
        public bool HasActiveQuota =>
            RemainingAds > 0 || RemainingPromotions > 0 || RemainingFeatures > 0 || RemainingDailyRefreshes > 0;

        public bool CanRefreshNow()
        {
            if (!CanRefreshAds || RemainingDailyRefreshes <= 0) return false;
            var since = DateTime.UtcNow - LastRefreshUsed;
            return since.TotalHours >= RefreshIntervalHours;
        }

        public void CheckAndResetDailyQuotas()
        {
            var today = DateTime.UtcNow.Date;
            if (LastDailyReset < today)
            {
                DailyRefreshesUsed = 0;
                LastDailyReset = today;
                LastUsageUpdate = DateTime.UtcNow;
            }
        }

        public ValidationResult ValidateAction(string actionType, int quantity = 1)
        {
            CheckAndResetDailyQuotas();
            var r = new ValidationResult { ActionType = actionType, RequestedQuantity = quantity, IsValid = false, Message = "Unknown action type" };

            switch (actionType.ToLower())
            {
                case ActionTypes.Publish:
                    r.IsValid = CanPublishAds && RemainingAds >= quantity;
                    r.RemainingQuota = RemainingAds;
                    r.Message = r.IsValid ? "Can publish" : (!CanPublishAds ? "Publishing not allowed" : "Insufficient ads quota");
                    break;
                case ActionTypes.Promote:
                    r.IsValid = CanPromoteAds && RemainingPromotions >= quantity;
                    r.RemainingQuota = RemainingPromotions;
                    r.Message = r.IsValid ? "Can promote" : (!CanPromoteAds ? "Promotion not allowed" : "Insufficient promotion quota");
                    break;
                case ActionTypes.Feature:
                    r.IsValid = CanFeatureAds && RemainingFeatures >= quantity;
                    r.RemainingQuota = RemainingFeatures;
                    r.Message = r.IsValid ? "Can feature" : (!CanFeatureAds ? "Featuring not allowed" : "Insufficient feature quota");
                    break;
                case ActionTypes.Refresh:
                    r.IsValid = CanRefreshNow() && RemainingDailyRefreshes >= quantity;
                    r.RemainingQuota = RemainingDailyRefreshes;
                    r.Message = r.IsValid ? "Can refresh" :
                        (!CanRefreshAds ? "Refresh not allowed" :
                        (!CanRefreshNow() ? $"Must wait {RefreshIntervalHours} hours" : "Insufficient daily refresh quota"));
                    break;
                case ActionTypes.SocialMediaPost:
                    r.IsValid = CanPostSocialMedia && RemainingSocialMediaPosts >= quantity;
                    r.RemainingQuota = RemainingSocialMediaPosts;
                    r.Message = r.IsValid ? "Can post" :
                        (!CanPostSocialMedia ? "Social posting not allowed" : "Insufficient social quota");
                    break;
            }
            return r;
        }

        public bool RecordUsage(string actionType, int quantity = 1, Dictionary<string, object>? metadata = null)
        {
            var v = ValidateAction(actionType, quantity);
            if (!v.IsValid) return false;

            switch (actionType.ToLower())
            {
                case ActionTypes.Publish: AdsUsed += quantity; break;
                case ActionTypes.Promote: PromotionsUsed += quantity; break;
                case ActionTypes.Feature: FeaturesUsed += quantity; break;
                case ActionTypes.Refresh: DailyRefreshesUsed += quantity; LastRefreshUsed = DateTime.UtcNow; break;
                case ActionTypes.SocialMediaPost: SocialMediaPostsUsed += quantity; break;
                default: return false;
            }

            LastUsageUpdate = DateTime.UtcNow;
            return true;
        }
    }
}
