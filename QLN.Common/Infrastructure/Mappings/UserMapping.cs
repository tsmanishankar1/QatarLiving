using QLN.Common.Infrastructure.Subscriptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.Mappings
{
    public class UserMapping
    {
        public Guid Id { get; set; }
        public string UserName { get; set; }
        public Guid UserId { get; set; }
        public IList<ShortSubscription> Subscriptions { get; set; }
        public IList<ShortBusinessProfile> BusinessProfiles { get; set; }
        public IList<ShortAddon> Addons { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; }
    }

    public class ShortBusinessProfile
    {
        public Guid Id { get; set; } 
        public string Name { get; set; }
    }

    public class ShortAddon
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }

    public class ShortSubscription
    {
        public Guid Id { get; set; }

        public SubscriptionName SubscriptionTypeId { get; set; }
        public string Name { get; set; }
    }
}
