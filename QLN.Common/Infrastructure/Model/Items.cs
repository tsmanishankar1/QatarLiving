using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.Model
{
    public class Items : ClassifiedBase
    {
        public DateTime? FeaturedExpiryDate { get; set; }
        public DateTime? PromotedExpiryDate { get; set; }
        public DateTime? LastRefreshedOn { get; set; }
        public bool IsFeatured { get; set; } = false;
        public bool IsPromoted { get; set; } = false;
        public bool IsRefreshed { get; set; } = false;
    }
   
}
