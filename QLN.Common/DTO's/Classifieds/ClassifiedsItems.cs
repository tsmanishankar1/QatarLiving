using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class ClassifiedsItems : ClassifiedsBase
    {
        public bool IsFeatured { get; set; }
        public DateTime? FeaturedExpiryDate { get; set; }
        public bool IsPromoted { get; set; }
        public DateTime? PromotedExpiryDate { get; set; }
        public bool IsRefreshed { get; set; }
        public DateTime? RefreshExpiryDate { get; set; }
    }

}
