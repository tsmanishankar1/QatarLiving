using QLN.Common.Infrastructure.Subscriptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.Classifieds
{
    public class ClassifiedsPayToPublish
    {
        public long AdId { get; set; }
        public Guid SubscriptionId { get; set; }
        public Vertical Vertical { get; set; }
        public SubVertical SubVertical { get; set; }
    }

    public class ClassifiedsPayToFeature
    {
        public long AdId { get; set; }
        public Guid AddonId { get; set; }
        public Vertical Vertical { get; set; }
        public SubVertical SubVertical { get; set; }
    }
}
