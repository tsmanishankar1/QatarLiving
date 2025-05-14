using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.Model
{
    public class BaseAdPostRequest
    {
        public string SubVertical { get; set; } // Items, Preloved, etc.
        public string Title { get; set; }
        public string Description { get; set; }

        public string Condition { get; set; } // Brand New / Used
        public string Gender { get; set; } // Optional, if applicable
        public decimal Price { get; set; }

        public string PhoneNumber { get; set; }
        public string WhatsAppNumber { get; set; }

        public string Zone { get; set; }
        public string StreetNumber { get; set; }
        public string BuildingNumber { get; set; }

        public List<IFormFile> Photos { get; set; } // Handle via multipart/form-data
    }
}
