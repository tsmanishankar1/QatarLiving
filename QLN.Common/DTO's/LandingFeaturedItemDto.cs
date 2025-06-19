using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static QLN.Common.DTO_s.ClassifiedsIndex;

namespace QLN.Common.DTO_s
{
    public class LandingFeaturedItemDto
    {
        public string Id { get; set; }
        public string Title { get; set; } = default!;
        public string? Description { get; set; }
        public string Category { get; set; } = default!;
        public double? Price { get; set; }
        public int Order { get; set; }
        public string? Color { get; set; }
        public string? Location { get; set; }
        public bool? IsFeatured { get; set; }
        public IList<ImageInfo>? ImageURLs { get; set; }
    }
}
