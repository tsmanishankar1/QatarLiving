namespace QLN.ContentBO.WebUI.Models
{
    public class FeaturedCategoryDto
    {
        public Guid? Id { get; set; }
        public Vertical Vertical { get; set; }
        public string Title { get; set; } = null!;
        public string CategoryName { get; set; } = null!;
        public long CategoryId { get; set; }
        public string L1categoryName { get; set; } = null!;
        public long L1CategoryId { get; set; }
        public int? SlotOrder { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public string ImageUrl { get; set; } = null!;
    }
}