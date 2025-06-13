using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class BannerCreateRequest
    {
        public string Category { get; set; }
        public string Code { get; set; }
        public string Alt { get; set; }
        public string Duration { get; set; }
        public string ImageDesktopBase64 { get; set; }    
        public string ImageDesktopUrl { get; set; }       
        public string ImageMobileBase64 { get; set; }     
        public string ImageMobileUrl { get; set; }      
        public string Link { get; set; }
        public string CreatedBy { get; set; }
    }
}
