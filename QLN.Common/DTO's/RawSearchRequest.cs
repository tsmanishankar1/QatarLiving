using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public sealed class RawSearchRequest
    {
        /// <summary> OData filter expression (required) </summary>
        public string Filter { get; set; } = default!;

        /// <summary> Optional comma-separated order by (e.g. "PublishedDate desc, CreatedAt desc") </summary>
        public string? OrderBy { get; set; }

        /// <summary> Page size (1..1000). Default 50 </summary>
        public int Top { get; set; } = 50;

        /// <summary> Offset (>= 0). Default 0 </summary>
        public int Skip { get; set; } = 0;

        /// <summary> Full-text search text (default "*") </summary>
        public string? Text { get; set; } = "*";
        public bool IncludeTotalCount { get; set; } = true;
    }
}
