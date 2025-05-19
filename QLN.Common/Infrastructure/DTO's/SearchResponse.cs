using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.DTO_s
{
    public class SearchResponse
    {
        public string VerticalName { get; set; } = string.Empty;
        public List<ClassifiedIndexDto>? ClassifiedsItems { get; set; }
    }

    /// <summary>
    /// Wrapper for a single‐item upload request.
    /// </summary>
    public class CommonIndexRequest
    {
        public string VerticalName { get; set; } = string.Empty;
        public ClassifiedIndexDto? ClassifiedsItem { get; set; }
    }
}
