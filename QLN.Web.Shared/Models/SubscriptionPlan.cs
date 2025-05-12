using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Web.Shared.Models
{
    public class SubscriptionPlan
    {
        public string Title { get; set; }
        public string Price { get; set; }
        public string Duration { get; set; }
        public int Flyers { get; set; }
    }

}
