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
        public UserAdCountsDto Counts { get; set; } = new();
    }

    public class UserAdCountsDto
    {
        public int ItemsPublished { get; set; }
        public int ItemsUnpublished { get; set; }
        public int PrelovedPublished { get; set; }
        public int PrelovedUnpublished { get; set; }
        public int CollectiblesPublished { get; set; }
        public int CollectiblesUnpublished { get; set; }
        public int DealsPublished { get; set; }
        public int DealsUnpublished { get; set; }
    }
}
