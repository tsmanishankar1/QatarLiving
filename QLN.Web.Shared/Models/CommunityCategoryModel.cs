namespace QLN.Web.Shared.Models
{
    public class CommunityCategoryResponse
    {
        public List<CommunityCategoryModel> ForumCategories { get; set; }
    }

    public class CommunityCategoryModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
}
