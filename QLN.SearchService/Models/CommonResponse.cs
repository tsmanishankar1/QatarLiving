using System.Collections.Generic;
using QLN.SearchService.IndexModels;

namespace QLN.SearchService.Models
{
    /// <summary>
    /// Wrapper for a search response: vertical name + list of ClassifiedIndex items.
    /// </summary>
    public class CommonResponse
    {
        public string VerticalName { get; set; } = string.Empty;
        public List<ClassifiedIndex>? ClassifiedsItems { get; set; }
    }

    /// <summary>
    /// Wrapper for a single‐item upload request.
    /// </summary>
    public class CommonIndexRequest
    {
        public string VerticalName { get; set; } = string.Empty;
        public ClassifiedIndex? ClassifiedsItem { get; set; }
    }
}
