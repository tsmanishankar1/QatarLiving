
using QLN.Common.DTO_s;
using QLN.Common.DTOs;
using System.Text.Json.Serialization;

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
    }

    /// <summary>
    /// Wrapper for a single‐item upload request.
    /// </summary>
    public class CommonIndexRequest
    {
        [JsonPropertyName("indexName")]
        public string IndexName { get; set; } = string.Empty;
        [JsonPropertyName("classifiedsItem")]
        public ClassifiedsItemsIndex? ClassifiedsItem { get; set; }
        [JsonPropertyName("classifiedsPrelovedItem")]
        public ClassifiedsPrelovedIndex? ClassifiedsPrelovedItem { get; set; }
        [JsonPropertyName("classifiedsCollectiblesItem")]
        public ClassifiedsCollectiblesIndex? ClassifiedsCollectiblesItem { get; set; }
        [JsonPropertyName("classifiedsDealsItem")]
        public ClassifiedsDealsIndex? ClassifiedsDealsItem { get; set; }
        [JsonPropertyName("servicesItem")]
        public ServicesIndex? ServicesItem { get; set; }
    }
}
