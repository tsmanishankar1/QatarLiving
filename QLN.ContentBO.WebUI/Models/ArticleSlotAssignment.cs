namespace QLN.ContentBO.WebUI.Models
{
    public class ArticleSlotAssignment
    {
        public int CategoryId { get; set; }
        public int SubCategoryId { get; set; }
        public int FromSlot { get; set; }
        public int ToSlot { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
    }

}
