namespace QLN.Common.Infrastructure.DTO_s
{
    public class NewsCategory
    {
        public int Id { get; set; }
        public string CategoryName { get; set; }
        public List<NewsSubCategory> SubCategories { get; set; } = new();
    }

    public class NewsSubCategory
    {
        public int Id { get; set; }
        public string SubCategoryName { get; set; }
    }
}
