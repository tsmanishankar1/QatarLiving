using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Web.Shared.Models
{
    public class SubscriptionResponse
    {
        public string VerticalId { get; set; } = "";
        public string VerticalName { get; set; } = "";
        public string CategoryId { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public List<SubscriptionPlan> Subscriptions { get; set; } = new();
    }


}
