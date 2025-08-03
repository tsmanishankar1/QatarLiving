using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class ClassifiedsBoItemsResponseDto
    {
        public long? TotalCount { get; set; }
        public List<ClassifiedsItems>? ClassifiedsItems { get; set; }
    }
    public class ClassifiedsBoPrelovedResponseDto
    {
        public long? TotalCount { get; set; }
        public List<ClassifiedsPreloved>? ClassifiedsPreloved { get; set; }
    }
    public class ClassifiedsBoCollectiblesResponseDto
    {
        public long? TotalCount { get; set; }
        public List<ClassifiedsCollectibles>? ClassifiedsCollectibles { get; set; }
    }
    public class ClassifiedsBoDealsResponseDto
    {
        public long? TotalCount { get; set; }
        public List<ClassifiedsDeals>? ClassifiedsDeals { get; set; }
    }
}
