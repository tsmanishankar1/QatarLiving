using QLN.Common.DTO_s;
using QLN.Common.DTO_s.Index;
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
        public List<ContentNewsIndex>? ContentNewsItems { get; set; }
        public List<ContentEventsIndex>? ContentEventsItems { get; set; }
        public List<ContentCommunityIndex>? ContentCommunityItems { get; set; }
        public List<ClassifiedStoresIndex>? ClassifiedStores { get; set; }
        public List<CompanyProfileIndex>? CompanyProfile { get; set; }
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
        [JsonPropertyName("contentNewsItem")]
        public ContentNewsIndex? ContentNewsItem { get; set; }
        [JsonPropertyName("contentEventsItem")]
        public ContentEventsIndex? ContentEventsItem { get; set; }
        [JsonPropertyName("contentCommunityItem")]
        public ContentCommunityIndex? ContentCommunityItem { get; set; }
        [JsonPropertyName("classifiedStores")]
        public ClassifiedStoresIndex? ClassifiedStores { get; set; }

        [JsonPropertyName("company")]
        public CompanyProfileIndex? CompanyProfile { get; set; }
    }
}
