using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.ClassifiedsBo
{
    public class GetAllSearch
    {
        public string Text { get; set; } = "*";
        public bool? IsFeatured { get; set; }
        public bool? IsPromoted { get; set; }
        public DateTime? CreatedAt { get; set; } 
        public DateTime? PublishedDate { get; set; }
        public AdStatus? Status { get; set; }
        public AdTypeEnum? AdType { get; set; }
        public string? OrderBy { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}
