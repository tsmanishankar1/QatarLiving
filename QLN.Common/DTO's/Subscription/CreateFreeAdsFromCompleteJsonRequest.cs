using QLN.Common.Infrastructure.Subscriptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.Subscription
{
    public class CreateFreeAdsFromCompleteJsonRequest
    {
        public string? ProductCode { get; set; }
        public string? ProductName { get; set; }
        public Vertical? Vertical { get; set; }
        public SubVertical? SubVertical { get; set; }
        public string? Currency { get; set; }
        public TimeSpan? Duration { get; set; }
        public string? Remarks { get; set; }
        /// <summary>
        /// Optional: Provide the complete JSON hierarchy here. If not provided, will read from file.
        /// </summary>
        public string? CategoryHierarchyJson { get; set; }
    }
    public class CategorySummaryDto
    {
        public string Category { get; set; } = string.Empty;
        public int SubcategoriesCount { get; set; }
        public int TotalAds { get; set; }
    }

    /// <summary>
    /// Strongly typed DTO for category quota details in API responses
    /// </summary>
    public class CategoryQuotaDto
    {
        public string CategoryPath { get; set; } = string.Empty;
        public int AdsBudget { get; set; }
    }
}
