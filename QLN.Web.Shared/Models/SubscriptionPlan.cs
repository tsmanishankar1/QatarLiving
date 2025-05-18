using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Web.Shared.Models
{
    public class SubscriptionPlan
    {
        public string Id { get; set; } = "";
        public string SubscriptionName { get; set; } = "";
        public decimal Price { get; set; }
        public string Currency { get; set; } = "";
        public string Duration { get; set; } = "";
        public string Description { get; set; } = "";


        public override int GetHashCode()
        {
            return HashCode.Combine(SubscriptionName, Price, Duration);
        }
    }

}
