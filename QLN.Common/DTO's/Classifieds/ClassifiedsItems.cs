using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class ClassifiedsItems : ClassifiedsBase
    {
        public bool IsFeatured { get; set; } = false;
        public DateTime? FeaturedExpiryDate { get; set; } = null;
        public bool IsPromoted { get; set; } = false;
        public DateTime? PromotedExpiryDate { get; set; } = null;
        public bool IsRefreshed { get; set; } = false;
        public DateTime? RefreshExpiryDate { get; set; } = null;
    }

}
