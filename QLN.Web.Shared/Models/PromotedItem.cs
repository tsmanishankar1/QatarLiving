namespace QLN.Web.Shared.Models
{
    public class PromotedItem
    {
        public List<string> Images { get; set; } = new();
        public string Category { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Price { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string CompanyLogoUrl { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Storage { get; set; } = string.Empty;
        public string Battery { get; set; } = string.Empty;
        public string Processor { get; set; } = string.Empty;
        public bool IsPromoted { get; set; }
        public bool IsFeatured { get; set; }
    }
}
