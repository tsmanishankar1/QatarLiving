using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.Payments
{
    public class D365Config
    {
        public string TenantId { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string D365Url { get; set; }
        public string D365ApiKey { get; set; }
        public string CheckoutPath { get; set; }
        public string InvoicePath { get; set; }
        public bool EnableD365Logs { get; set; }
        public List<string> D365ErrorEmails { get; set; }
    }
}
