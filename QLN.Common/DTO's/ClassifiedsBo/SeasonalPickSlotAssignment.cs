using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class SeasonalPickSlotAssignment
    {
        public int SlotOrder { get; set; }
        public string? PickId { get; set; }
    }

    public class SeasonalPickSlotReorderRequest
    {
        public List<SeasonalPickSlotAssignment> SlotAssignments { get; set; } = new();
        public string Vertical { get; set; }
    }

    public class ReplaceSeasonalPickSlotRequest
    {
        public string PickId { get; set; }
        public int TargetSlotId { get; set; }
        public string Vertical { get; set; }
    }
}
