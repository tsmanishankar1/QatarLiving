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
        public DateTime DateCreated { get; set; }
        public string Weburl { get; set; }
        public int WebClick { get; set; }
        public int Views { get; set; }
        public int Impression { get; set; }
        public int Phonelead { get; set; }
        public string email { get; set; }
        public string createdby { get; set; }
        public string ContactNumber { get; set; }
        public string WhatsappNumber { get; set; }
        public List<string> Location { get; set; } = new List<string>();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
