using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.DTO_s
{
    public class BannerImageUpdateDto
    {
        public string? Title { get; set; }
        public string? Alt { get; set; }
        public string? Href { get; set; }
        public int? Duration { get; set; }
        public bool? IsDesktop { get; set; }
        public bool? IsMobile { get; set; }
        public int? SortOrder { get; set; }
        public string? AnalyticsSlot { get; set; }
        public IFormFile? File { get; set; }
    }
}
