using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class GenericUserQuotaDto
    {
        public Guid Id { get; set; }
        public string UserId { get; set; }

        public Guid? PaymentTransactionId { get; set; }
        public Guid? AdId { get; set; }

        public string? SourceType { get; set; }

        public Guid? SubscriptionId { get; set; }
        public string? SubscriptionName { get; set; }

        public Guid? AddonId { get; set; }
        public string? AddonName { get; set; }

        public int? VerticalTypeId { get; set; }
        public string? VerticalName { get; set; }
        public int? SubVerticalId { get; set; }
        public string? SubVerticalName { get; set; }

        public int? AdRefreshUsage { get; set; }

        public int? TotalAdBudget { get; set; }
        public int? UsedAdBudget { get; set; }

        public int? TotalPromoteBudget { get; set; }
        public int? UsedPromoteBudget { get; set; }

        public int? TotalFeatureBudget { get; set; }
        public int? UsedFeatureBudget { get; set; }

        public int? TotalRefreshBudget { get; set; }
        public int? UsedRefreshBudget { get; set; }
        public int? DailyRefreshQuota { get; set; }
        public int? UsedRefreshToday { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public string? Currency { get; set; }
        public decimal? Price { get; set; }

        public string? CardHolderName { get; set; }
        public DateTime TransactionDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    public class UserQuotaCollection
    {
        public string UserId { get; set; }
        public List<GenericUserQuotaDto> Quotas { get; set; } = new();
    }
}
