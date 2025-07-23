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
        public List<ClassifiedsItemsIndex>? ClassifiedsItems { get; set; }
    }
}
