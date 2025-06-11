namespace QLN.Web.Shared.Models
{
    public class CategoryItem
    {
        public string Icon { get; set; }
        public string Label { get; set; }
        public string QLCategLink { get; set; }  
        public string CategorySlug { get; set; }
        public string SubcategorySlug { get; set; } // optional
    }
}
