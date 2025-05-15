using QLN.SearchService.IndexModels;

namespace QLN.SearchService.Models
{
    public class CommonIndexModel
    {
        public string VerticalName { get; set; } = string.Empty;
        public ClassifiedIndex? Classified { get; set; }

    }
}
