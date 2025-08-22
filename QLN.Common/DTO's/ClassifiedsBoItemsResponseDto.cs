using QLN.Common.Infrastructure.Model;
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
        public List<Items>? ClassifiedsItems { get; set; }
    }
    public class ClassifiedsBoPrelovedResponseDto
    {
        public long? TotalCount { get; set; }
        public List<Preloveds>? ClassifiedsPreloved { get; set; }
    }
    public class ClassifiedsBoCollectiblesResponseDto
    {
        public long? TotalCount { get; set; }
        public List<Collectibles>? ClassifiedsCollectibles { get; set; }
    }
    public class ClassifiedsBoDealsResponseDto
    {
        public long? TotalCount { get; set; }
        public List<ClassifiedsDeals>? ClassifiedsDeals { get; set; }
    }
}
