using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.Company
{
    public class CompanySubscriptionDto
    {
        public string CompanyName { get; set; }
        public string Email { get; set; }
        public string Mobile { get; set; }
        public string WhatsApp { get; set; }
        public string WebUrl { get; set; }
        public string SubscriptionStatus { get; set; }
        public DateTime SubscriptionStartDate { get; set; }
        public DateTime SubscriptionEndDate { get; set; }
    }
    public class CompanySubscriptionFilter
    {
        public string ProductName { get; set; }
        public DateTime? StartDate { get; set; } 
        public DateTime? EndDate { get; set; }  
    }
}
