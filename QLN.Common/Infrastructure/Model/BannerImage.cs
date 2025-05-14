using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.Model
{
    public class BannerImage
    {
        public Guid Id { get; set; }
        public string AnalyticsSlot { get; set; } = null!;
        public string Alt { get; set; } = null!;
        public string ImageDesktop { get; set; } = null!;
        public string ImageMobile { get; set; } = null!;
        public string? Href { get; set; }
        public string? WidthDesktop { get; set; }
        public string? HeightDesktop { get; set; }
        public string? WidthMobile { get; set; }
        public string? HeightMobile { get; set; }
        public string? Title { get; set; }
        public int Duration { get; set; }
        public int? SortOrder { get; set; }
        public bool? IsDesktop { get; set; }
        public bool? IsMobile { get; set; }
        public Guid BannerId { get; set; }
        [JsonIgnore]
        public Banner Banner { get; set; } = null!;
    }
}
