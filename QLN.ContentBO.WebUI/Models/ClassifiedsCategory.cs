namespace QLN.ContentBO.WebUI.Models
{
    public class ClassifiedsCategory
    {
        public long Id { get; set; }
        public string CategoryName { get; set; }
        public string Vertical { get; set; }
        public int? ParentId { get; set; }
        public string SubVertical { get; set; }
        public List<ClassifiedsCategoryField> Fields { get; set; } = new();
    }
    public class ClassifiedsCategoryField
    {
        public int Id { get; set; }
        public string CategoryName { get; set; }
        public string Type { get; set; }
        public List<string> Options { get; set; } = new();
        public List<ClassifiedsCategoryField> Fields { get; set; } = new(); 
    }

}