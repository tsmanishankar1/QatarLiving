using System.ComponentModel.DataAnnotations;

namespace QLN.ContentBO.WebUI.Models
{
    public class BulkModerationRequest
    {
        public List<long> AdIds { get; set; } = new();
        public BulkModerationAction Action { get; set; }
        public string? Reason { get; set; }
        public string? UpdatedBy { get; set; }
    }
    public enum BulkModerationAction
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
    Onhold = 11,
    IsRefreshed = 12
  }
}
