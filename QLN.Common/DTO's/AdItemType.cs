using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.DTO_s
{
    public enum AdItemType
    {
        Category,
        SubCategory,
        Brand,
        Model,
        Color,
        Condition,
        Capacity,
        Processor,
        Coverage,
        Ram,
        Resolution,
        SizeType,
        Gender,
        Zone
    }

    public class BaseItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public AdItemType Type { get; set; }
    }
}
