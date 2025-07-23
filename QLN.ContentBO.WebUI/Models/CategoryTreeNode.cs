namespace QLN.ContentBO.WebUI.Models
{
    public class CategoryTreeNode
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<Field>? Fields { get; set; }
        public List<CategoryTreeNode>? Children { get; set; }
    }

    public class Field
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public List<string> Options { get; set; }
    }

}
