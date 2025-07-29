using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class ClassifiedsItems : ClassifiedsBase
    {
        public DateTime? FeaturedExpiryDate { get; set; } = null;
        public DateTime? PromotedExpiryDate { get; set; } = null;
        public DateTime? LastRefreshedOn { get; set; } = null;
        public bool IsFeatured {get; set;}
        public bool IsPromoted { get; set;}
        public bool IsRefreshed => LastRefreshedOn.HasValue && LastRefreshedOn.Value > DateTime.UtcNow;
    }

}
