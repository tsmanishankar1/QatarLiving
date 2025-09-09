namespace QLN.ContentBO.WebUI.Models
{
    public class BannerDropDownModel
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
    public class BannerTypeOption
    {
        public string Page { get; set; } = string.Empty;
        public List<BannerDropDownModel> Types { get; set; } = new();
    }
}
