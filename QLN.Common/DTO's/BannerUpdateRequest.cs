using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class BannerUpdateRequest
    {
        public string Category { get; set; }
        public string Code { get; set; }
        // Fields to update
        public string? Alt { get; set; }
        public string? Duration { get; set; }
        public string? Link { get; set; }
        public string? ImageDesktopBase64 { get; set; }
        public string? ImageMobileBase64 { get; set; }
        public string UpdatedBy { get; set; }
    }

    // For delete
    public class BannerDeleteRequest
    {
        public string Category { get; set; }
        public string Code { get; set; }
    }
}
