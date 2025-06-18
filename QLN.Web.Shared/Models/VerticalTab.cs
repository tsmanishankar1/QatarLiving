namespace QLN.Web.Shared.Models
{
    public class VerticalTab
    {
        public int Index { get; set; }
        public int VerticalId { get; set; }
        public string Label { get; set; }
        public string Icon { get; set; }
        public List<CategoryTab> Categories { get; set; } = new();
    }
    public class CategoryTab
    {
        public int Index { get; set; }
        public int CategoryId { get; set; }
        public string Label { get; set; }
        public string Icon { get; set; }
    }


}
