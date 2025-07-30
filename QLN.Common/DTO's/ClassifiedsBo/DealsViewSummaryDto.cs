using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.ClassifiedsBo
{
    public class DealsViewSummaryDto
    {
        public Guid AdId { get; set; }
        public string Dealtitle { get; set; }
        public string subscriptiontype { get; set; }
        public string DateCreated { get; set; }
        public string Weburl { get; set; }
        public string WebClick { get; set; }
        public string Views { get; set; }
        public string Impression { get; set; }
        public string Phonelead { get; set; }
        public string email { get; set; }
        public string createdby { get; set; }
        public string ContactNumber { get; set; }
        public string WhatsappNumber { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
    }
}
