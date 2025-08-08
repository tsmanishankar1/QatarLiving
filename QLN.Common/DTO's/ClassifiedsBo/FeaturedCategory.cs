using QLN.Common.Infrastructure.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class FeaturedCategory
    {
        public Guid? Id { get; set; }//
        public string CategoryName { get; set; } = null!;
        public string CategoryId { get; set; } = null!;
        public string Vertical { get; set; } = null!;
        public string? UserId { get; set; }//
        public string? UserName { get; set; }// 
        public bool? IsActive { get; set; }
        [JsonConverter(typeof(FlexibleDateOnlyConverter))]
        public DateOnly StartDate { get; set; }
        [JsonConverter(typeof(FlexibleDateOnlyConverter))]
        public DateOnly EndDate { get; set; }
        public int SlotOrder { get; set; }
        public string ImageUrl { get; set; }
        public DateTime? CreatedAt { get; set; }//
        public DateTime? UpdatedAt { get; set; }
    }
    public class LandingBoSlotAssignment
    {
        public int SlotOrder { get; set; }
        public string? CategoryId { get; set; }
    }

    public class LandingBoSlotReorderRequest
    {
        public List<LandingBoSlotAssignment> SlotAssignments { get; set; } = new();
        public string Vertical { get; set; }
    }


    public class LandingBoSlotReplaceRequest
    {
        public string CategoryId { get; set; }
        public int TargetSlotId { get; set; }
        public string Vertical { get; set; }

    }

    public class BulkActionRequest
    {
       public List<long> AdIds { get; set; } = new();
      
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
