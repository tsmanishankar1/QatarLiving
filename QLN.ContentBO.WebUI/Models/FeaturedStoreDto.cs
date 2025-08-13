namespace QLN.ContentBO.WebUI.Models
{
    public class FeaturedStoreDto
    {
        public Guid? Id { get; set; }
        public string Title { get; set; } = null!;
        public Vertical Vertical { get; set; }
        public string StoreId { get; set; } = null!;
        public string StoreName { get; set; } = null!;
        public string ImageUrl { get; set; } = null!;
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
    }
}