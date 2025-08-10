using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.Subscription
{
    public class V2UserAddonRequestDto
    {
        public string AddonName { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public Guid? SubscriptionId { get; set; }
        public decimal PromoteBudget { get; set; }
        public decimal FeatureBudget { get; set; }
        public decimal RefreshBudget { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; } = "QAR";
        public TimeSpan Duration { get; set; }
        public V2Status StatusId { get; set; } = V2Status.Active;
    }
}
