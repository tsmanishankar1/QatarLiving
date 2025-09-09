using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s.Payments;
using QLN.Common.DTO_s.Subscription;
using QLN.Common.Infrastructure.CustomException;
using QLN.Common.Infrastructure.Model;
using QLN.Common.Infrastructure.Subscriptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.Subscription
{
    public class FreeAdsProductConstraints
    {
        public TimeSpan? Duration { get; set; } 
        public string Scope { get; set; } = "Category-Based";
        public List<FreeAdsCategoryQuota> CategoryQuotas { get; set; } = new List<FreeAdsCategoryQuota>();
        public string? Remarks { get; set; }
    }
    public class FreeAdsCategoryQuota
    {
        public string Category { get; set; } = string.Empty; // e.g., "Electronics"
        public string? L1Category { get; set; } // e.g., "Home appliances"
        public string? L2Category { get; set; } // e.g., "ACs"
        public int FreeAdsAllowed { get; set; } // Free ads allowed in this category
    }
    public class FreeAdsSubscriptionQuota
    {
        public string Vertical { get; set; } = string.Empty;
        public string Scope { get; set; } = "Category-Based";

        // Only free ads tracking - no features, promotions, refreshes
        public List<FreeAdsCategoryUsage> CategoryQuotas { get; set; } = new List<FreeAdsCategoryUsage>();

        // Validation method for free ads only
        public FreeAdsValidationResult ValidateFreeAdsUsage(int quantity, string category, string? l1Category = null, string? l2Category = null)
        {
            var categoryUsage = GetOrCreateCategoryUsage(category, l1Category, l2Category);

            if (categoryUsage.FreeAdsUsed + quantity > categoryUsage.FreeAdsAllowed)
            {
                return new FreeAdsValidationResult
                {
                    IsValid = false,
                    Message = $"Free ads quota exceeded for {categoryUsage.GetCategoryPath()}. Used: {categoryUsage.FreeAdsUsed}, Allowed: {categoryUsage.FreeAdsAllowed}",
                    RemainingQuota = categoryUsage.GetRemainingFreeAds()
                };
            }

            return new FreeAdsValidationResult
            {
                IsValid = true,
                Message = "Free ads quota available",
                RemainingQuota = categoryUsage.GetRemainingFreeAds() - quantity
            };
        }

        // Record free ads usage
        public bool RecordFreeAdsUsage(int quantity, string category, string? l1Category = null, string? l2Category = null)
        {
            var validation = ValidateFreeAdsUsage(quantity, category, l1Category, l2Category);
            if (!validation.IsValid)
                return false;

            var categoryUsage = GetOrCreateCategoryUsage(category, l1Category, l2Category);
            categoryUsage.FreeAdsUsed += quantity;
            return true;
        }

        // Get category usage summary
        public List<FreeAdsCategorySummary> GetCategoryUsageSummary()
        {
            return CategoryQuotas.Select(c => new FreeAdsCategorySummary
            {
                CategoryPath = c.GetCategoryPath(),
                FreeAdsAllowed = c.FreeAdsAllowed,
                FreeAdsUsed = c.FreeAdsUsed,
                FreeAdsRemaining = c.GetRemainingFreeAds(),
                UsagePercentage = c.FreeAdsAllowed > 0 ? (double)c.FreeAdsUsed / c.FreeAdsAllowed * 100 : 0
            }).ToList();
        }

        private FreeAdsCategoryUsage GetOrCreateCategoryUsage(string category, string? l1Category, string? l2Category)
        {
            var existing = CategoryQuotas.FirstOrDefault(c =>
                c.Category == category &&
                c.L1Category == l1Category &&
                c.L2Category == l2Category);

            if (existing != null)
                return existing;

            // This shouldn't happen in normal flow, but create a default entry
            var newUsage = new FreeAdsCategoryUsage
            {
                Category = category,
                L1Category = l1Category,
                L2Category = l2Category,
                FreeAdsAllowed = 0 // Will be set from product constraints
            };

            CategoryQuotas.Add(newUsage);
            return newUsage;
        }
    }

    // Category usage tracking for free ads
    public class FreeAdsCategoryUsage
    {
        public string Category { get; set; } = string.Empty;
        public string? L1Category { get; set; }
        public string? L2Category { get; set; }

        // Only free ads tracking
        public int FreeAdsAllowed { get; set; }
        public int FreeAdsUsed { get; set; }

        public string GetCategoryPath()
        {
            if (!string.IsNullOrEmpty(L2Category))
                return $"{Category} > {L1Category} > {L2Category}";
            if (!string.IsNullOrEmpty(L1Category))
                return $"{Category} > {L1Category}";
            return Category;
        }

        public int GetRemainingFreeAds() => Math.Max(0, FreeAdsAllowed - FreeAdsUsed);
        public bool HasRemainingQuota() => GetRemainingFreeAds() > 0;
        public double GetUsagePercentage() => FreeAdsAllowed > 0 ? (double)FreeAdsUsed / FreeAdsAllowed * 100 : 0;
    }

    // Validation result for free ads
    public class FreeAdsValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public int RemainingQuota { get; set; }
    }

    // Category summary for reporting
    public class FreeAdsCategorySummary
    {
        public string CategoryPath { get; set; } = string.Empty;
        public int FreeAdsAllowed { get; set; }
        public int FreeAdsUsed { get; set; }
        public int FreeAdsRemaining { get; set; }
        public double UsagePercentage { get; set; }
    }

    // DTOs for free ads validation and recording
    public class FreeAdsValidationRequest
    {
        public Guid SubscriptionId { get; set; }
        public int RequestedAmount { get; set; } = 1;
        public string Category { get; set; } = string.Empty; // e.g., "Electronics"
        public string? L1Category { get; set; } // e.g., "Home appliances"
        public string? L2Category { get; set; } // e.g., "ACs"
    }

    public class FreeAdsRecordRequest
    {
        public Guid SubscriptionId { get; set; }
        public int Amount { get; set; } = 1;
        public string Category { get; set; } = string.Empty;
        public string? L1Category { get; set; }
        public string? L2Category { get; set; }
    }

    public class FreeAdsValidationResponse
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public Guid SubscriptionId { get; set; }
        public int RequestedAmount { get; set; }
        public string CategoryPath { get; set; } = string.Empty;
        public int RemainingQuota { get; set; }
        public string Version { get; set; } = "V2";
    }

    public class FreeAdsRecordResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Guid SubscriptionId { get; set; }
        public int AmountRecorded { get; set; }
        public string CategoryPath { get; set; } = string.Empty;
        public int RemainingQuota { get; set; }
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
        public string Version { get; set; } = "V2";
    }

    // DTO for creating FREE ads product
    public class CreateFreeAdsProductDto
    {
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public Vertical Vertical { get; set; } = Vertical.Classifieds; // Usually Classifieds for free ads
        public SubVertical? SubVertical { get; set; }
        public decimal Price { get; set; } = 0; // Always 0 for FREE products
        public string Currency { get; set; } = "QAR";
        public TimeSpan? Duration { get; set; } // Usually unlimited
        public string? CategoryHierarchyJson { get; set; } // JSON from your file
        public string? Remarks { get; set; }
    }

    // JSON parsing models for the provided hierarchy
    public class CategoryHierarchy
    {
        public string Category { get; set; } = string.Empty;
        public List<L1Category> L1 { get; set; } = new List<L1Category>();
    }

    public class L1Category
    {
        [JsonPropertyName("l1category")]
        public string L1CategoryName { get; set; } = string.Empty;

        [JsonPropertyName("l1cap")]
        public int L1Cap { get; set; }

        public List<L2Category>? L2 { get; set; }
    }

    public class L2Category
    {
        [JsonPropertyName("l2category")]
        public string L2CategoryName { get; set; } = string.Empty;

        [JsonPropertyName("adsbudget")]
        public int AdsBudget { get; set; }
    }
}

