using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class ServicesCategoryDto
    {
        public Guid? Id { get; set; }
        public string Category { get; set; } = default!;
        public List<L1CategoryDto> L1Categories { get; set; } = new();
    }
    public class L1CategoryDto
    {
        public Guid Id { get; set; } 
        public string Name { get; set; } = default!;
        public List<L2CategoryDto> L2Categories { get; set; } = new();
    }
    public class L2CategoryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
    }
}
