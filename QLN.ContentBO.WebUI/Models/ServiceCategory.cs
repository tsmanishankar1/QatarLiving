
namespace QLN.ContentBO.WebUI.Models
{
    public class ServiceCategory
    {
        public long Id { get; set; }
        public string CategoryName { get; set; } = default!;
        public List<L1Category> Fields { get; set; } = new();
    }

    public class L1Category
    {
        public long Id { get; set; } 
        public string CategoryName { get; set; } = default!;
        public List<L2Category> Fields { get; set; } = new();
    }

    public class L2Category
    {
        public long Id { get; set; }
        public string CategoryName { get; set; } = default!;
    }

}

 