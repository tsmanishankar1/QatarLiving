using QLN.Common.Infrastructure.Subscriptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.Subscription
{
    public class V2SubscriptionCancelledEventDto
    {
        public Guid SubscriptionId { get; set; }
        public string ProductCode { get; set; }
        public string UserId { get; set; }
        public Vertical Vertical { get; set; }
        public SubVertical? SubVertical { get; set; }
        public DateTime CancelledAt { get; set; }
        public Guid EventId { get; set; }
        public string Version { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }
}
