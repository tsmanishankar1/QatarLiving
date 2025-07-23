using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class FeaturedStoreSlotAssignment
    {
        public int SlotNumber { get; set; }
        public string? StoreId { get; set; }
    }

    public class FeaturedStoreSlotReorderRequest
    {
        public List<FeaturedStoreSlotAssignment> SlotAssignments { get; set; } = new();
        public string? UserId { get; set; }
        public string? Vertical { get; set; }
    }
}
