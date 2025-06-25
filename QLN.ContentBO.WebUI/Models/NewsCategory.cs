namespace QLN.ContentBO.WebUI.Models
{
    public class NewsCategory
    {
        public int Id { get; set; }
        public string CategoryName { get; set; }
        public List<NewsSubCategory> SubCategories { get; set; }
    }
}
