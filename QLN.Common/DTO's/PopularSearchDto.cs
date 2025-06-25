using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace QLN.Common.DTO_s
{
    public class PopularSearchDto
    {
        public string Id { get; set; } = default!;
        public string Title { get; set; } = default!;
        public string? ParentId { get; set; }
        public string Vertical { get; set; } = default!;
        public string EntityType { get; set; } = default!;
        public int Order { get; set; }
        public string? RediectUrl { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; }
        public IEnumerable<PopularSearchDto> Children { get; set; } = Enumerable.Empty<PopularSearchDto>();
        
        [JsonIgnore]
        public bool IsExpanded { get; set; } = false;
    }
}
