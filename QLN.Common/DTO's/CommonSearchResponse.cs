
using QLN.Common.DTO_s;
using QLN.Common.DTOs;

namespace QLN.Common.DTO_s
{
    /// <summary>
    /// Wrapper for a search response: vertical name + list of ClassifiedIndex items.
    /// </summary>
    public class CommonSearchResponse
    {
        public string VerticalName { get; set; } = string.Empty;
        public List<ClassifiedsIndex>? ClassifiedsItems { get; set; }
        public List<ServicesIndex>? ServicesItems { get; set; }
        public List<BackofficemasterIndex>? MasterItems { get; set; }
    }

    /// <summary>
    /// Wrapper for a single‐item upload request.
    /// </summary>
    public class CommonIndexRequest
    {
        public string VerticalName { get; set; } = string.Empty;
        public ClassifiedsIndex? ClassifiedsItem { get; set; }
        public ServicesIndex? ServicesItem { get; set; }
        public BackofficemasterIndex? MasterItem { get; set; }
    }
}
