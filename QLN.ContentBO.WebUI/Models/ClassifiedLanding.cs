namespace QLN.ContentBO.WebUI.Models
{
    public class ClassifiedLanding
    {
        public class LandingPageItem
        {
            public Guid? Id { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public string Category { get; set; }
            public string Subcategory { get; set; }
            public string Section { get; set; }
            public int SlotOrder {  get; set; }
            public bool IsPlaceholder {  get; set; }
            public DateTime? EndDate { get; set; }
            public string ImageUrl { get; set; }
        }

        public class Slot
        {
            public int SlotNumber { get; set; }
            public LandingPageItem? Event { get; set; }
        }

        public class SeasonalPickDto
        {
            public string Id { get; set; } = "";
            public string Title { get; set; } = "";
            public string CategoryName { get; set; } = "";
            public string? StoreName { get; set; } = "";
            public DateTime EndDate { get; set; }
            public int SlotOrder { get; set; }
        }
    }
}
