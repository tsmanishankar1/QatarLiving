using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.Model
{
    public class DealsAdPostRequest : BaseAdPostRequest
    {
        public string OfferTitle { get; set; }
        public IFormFile FlyerFile { get; set; } // Must be PDF
        public string XmlLink { get; set; }
        public DateTime ExpiryDate { get; set; }
    }
}
