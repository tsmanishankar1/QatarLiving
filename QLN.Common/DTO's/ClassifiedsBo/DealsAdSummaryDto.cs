using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.ClassifiedsBo
{
    public class DealsAdSummaryDto
    {
        public Guid AdId { get; set; }
        public string orderid { get; set; }
        public string subscriptiontype { get; set; }
        public string status { get; set; }
        public string price { get; set; }
        public string email { get; set; }
        public string createdby { get; set; }
        public string ContactNumber { get; set; }
        public string WhatsappNumber { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string UserName { get; set; }
    }
}
