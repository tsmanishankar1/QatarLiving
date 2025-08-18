using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Subscriptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.Subscription
{
    public class V2AddonCancelledEventDto
    {
        public Guid AddonId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public Guid? SubscriptionId { get; set; }
        public Vertical Vertical { get; set; }
        public SubVertical? SubVertical { get; set; }
        public DateTime CancelledAt { get; set; }
        public Guid EventId { get; set; }
        public string Version { get; set; } = "V2";
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}
