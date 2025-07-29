namespace QLN.ContentBO.WebUI.Models
{
    public class ReorderRequest
    {
        public List<ArticleSlotAssignment> SlotAssignments { get; set; } = new();
        public int CategoryId { get; set; }
        public int SubCategoryId { get; set; }
        public string UserId { get; set; }
    }
}
