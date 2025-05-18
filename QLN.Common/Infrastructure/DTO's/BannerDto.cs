using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.DTO_s
{
    public class BannerDto
    {
        public string Display { get; set; } = null!;
        public string WidthDesktop { get; set; } = null!;
        public string HeightDesktop { get; set; } = null!;
        public string WidthMobile { get; set; } = null!;
        public string HeightMobile { get; set; } = null!;
        public string? Text { get; set; }
        public string Rotation { get; set; } = null!;
    }
}
