using QLN.Common.DTO_s;
using QLN.Common.DTOs;

namespace QLN.Common.DTO_s
{
    /// <summary>
    /// Wrapper for a search response: vertical name + result list.
    /// </summary>
    public class CommonSearchResponse
    {
        public string VerticalName { get; set; } = string.Empty;
        public string? SubVertical { get; set; } = string.Empty;
        public long? TotalCount { get; set; }
        public List<ClassifiedsItemsIndex>? Items { get; set; }
        public List<ClassifiedsPrelovedIndex>? Preloved { get; set; }
        public List<ClassifiedsCollectiblesIndex>? Collectibles { get; set; }
        public List<ClassifiedsDealsIndex>? Deals { get; set; }
        public List<ServicesIndex>? ServicesItems { get; set; }
        public List<LandingBackOfficeIndex>? MasterItems { get; set; }
    }

    /// <summary>
    /// Wrapper for a single‐item upload request.
    /// </summary>
    public class CommonIndexRequest
    {
        public string VerticalName { get; set; } = string.Empty;
        public string? SubVertical { get; set; }
        public ClassifiedsItemsIndex? Items { get; set; }
        public ClassifiedsPrelovedIndex? Preloved { get; set; }
        public ClassifiedsCollectiblesIndex? Collectibles { get; set; }
        public ClassifiedsDealsIndex? Deals { get; set; }

        public ServicesIndex? ServicesItem { get; set; }
        public LandingBackOfficeIndex? MasterItem { get; set; }
    }
}
