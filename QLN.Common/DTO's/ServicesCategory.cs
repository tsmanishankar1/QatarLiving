using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class ServicesCategory
    {
        public Guid? Id { get; set; }
        public string MainCategory { get; set; } = default!;
        public List<L1Category> L1Categories { get; set; } = new();
    }
    public class L1Category
    {
        public Guid Id { get; set; } 
        public string Name { get; set; } = default!;
        public List<L2Category> L2Categories { get; set; } = new();
    }
    public class L2Category
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
    }
}
