using QLN.Common.Infrastructure.Subscriptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.ClassifiedsBo
{
    public class LandingBoSlotAssignment
    {
        public int SlotOrder { get; set; }
        public string CategoryId { get; set; } = null!;
    }

    public class LandingBoSlotReorderRequest
    {
        public List<LandingBoSlotAssignment> SlotAssignments { get; set; } = new();
        public Vertical Vertical { get; set; }
    }


    public class LandingBoSlotReplaceRequest
    {
        public string CategoryId { get; set; } = null!;
        public int TargetSlotId { get; set; }
        public Vertical Vertical { get; set; }

    }

    public class BulkActionRequest
    {
        public List<Guid> AdIds { get; set; } = new();
        public BulkActionEnum Action { get; set; }
        public string? Reason { get; set; }
        public string? Comments { get; set; }
    }
    public enum BulkActionEnum
    {
        Approve = 1,
        Publish = 2,
        Unpublish = 3,
        UnPromote = 4,
        UnFeature = 5,
        Remove = 6,
        NeedChanges = 7,
        Promote = 8,
        Feature = 9,
        Hold = 10,
        Onhold = 11
    }
}
