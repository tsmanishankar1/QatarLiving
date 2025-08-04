
namespace QLN.ContentBO.WebUI.Models
{
    public class ServiceCategory
    {
        public Guid Id { get; set; }
        public string Category { get; set; } = default!;
        public List<L1Category> L1Categories { get; set; } = new();
    }

    public class L1Category
    {
        public Guid Id { get; set; } 
        public string Name { get; set; } = default!;
        public List<L2Category> L2Categories { get; set; } = new();
    }

    public class L2Category
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
    }

}

 