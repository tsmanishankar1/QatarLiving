using System.ComponentModel.DataAnnotations;

namespace QLN.ContentBO.WebUI.Models
{
    public class BulkModerationRequest
    {
        public List<Guid> AdIds { get; set; } = new();
        public BulkModerationAction Action { get; set; }
        public string? Reason { get; set; }
        public string? UpdatedBy { get; set; }
    }
  public enum BulkModerationAction
    {
        Approve = 1,
        Publish = 2,
        Unpublish = 3,
        Remove = 4
    }
}
