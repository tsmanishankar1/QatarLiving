using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static QLN.Common.DTO_s.ClassifiedsIndex;

namespace QLN.Common.DTO_s
{
    public class PaginatedAdResponseDto
    {
        public int Total { get; set; }
        public List<object> Items { get; set; } = new();
    }
}
