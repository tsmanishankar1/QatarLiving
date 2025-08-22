using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.Services
{
    public class PayToPromote
    {
        public long ServiceId { get; set; }
        public Guid AddonId { get; set; }
    }
    public class PayToFeature
    {
        public long ServiceId { get; set; }
        public Guid AddonId { get; set; }
    }
    public class PayToPublish
    {
        public long ServiceId { get; set; }
        public Guid SubscriptionId { get; set; }
    }
}
