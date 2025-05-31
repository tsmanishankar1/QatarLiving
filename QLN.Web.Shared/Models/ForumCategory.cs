namespace QLN.Web.Shared.Model
{
    public class ForumCategory
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
    public class CategoryResponse
    {
        public List<ForumCategory> Forum_Categories { get; set; }
    }

}
