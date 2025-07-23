
using QLN.Common.DTO_s;
using QLN.Common.DTOs;

namespace QLN.Common.DTO_s
{
    /// <summary>
    /// Wrapper for a search response: vertical name + list of ClassifiedIndex items.
    /// </summary>
    public class CommonSearchResponse
    {
        public long? TotalCount { get; set; }
        public List<ClassifiedsItemsIndex>? ClassifiedsItem { get; set; }
        public List<ClassifiedsPrelovedIndex>? ClassifiedsPrelovedItem { get; set; }
        public List<ClassifiedsCollectiblesIndex>? ClassifiedsCollectiblesItem { get; set; }
        public List<ClassifiedsDealsIndex>? ClassifiedsDealsItem { get; set; }
        public List<ServicesIndex>? ServicesItems { get; set; }
        public List<LandingBackOfficeIndex>? MasterItems { get; set; }
    }

    /// <summary>
    /// Wrapper for a single‐item upload request.
    /// </summary>
    public class CommonIndexRequest
    {
        public string IndexName { get; set; } = string.Empty;
        public ClassifiedsItemsIndex? ClassifiedsItem { get; set; }
        public ClassifiedsPrelovedIndex? ClassifiedsPrelovedItem { get; set; }
        public ClassifiedsCollectiblesIndex? ClassifiedsCollectiblesItem { get; set; }
        public ClassifiedsDealsIndex? ClassifiedsDealsItem { get; set; }
        public ServicesIndex? ServicesItem { get; set; }
        public LandingBackOfficeIndex? MasterItem { get; set; }
    }
}
