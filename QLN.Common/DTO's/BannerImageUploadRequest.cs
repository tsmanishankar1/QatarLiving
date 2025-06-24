using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.DTO_s
{
    public class BannerImageUploadRequest
    {
        [Required]
        [FromForm(Name = "file")]
        public IFormFile File { get; set; }
        [FromForm] public Guid BannerId { get; set; }
        [FromForm] public string AnalyticsSlot { get; set; }
        [FromForm] public string Alt { get; set; }
        [FromForm] public int Duration { get; set; }
        [FromForm] public string Href { get; set; }
        [FromForm] public string WidthDesktop { get; set; }
        [FromForm] public string HeightDesktop { get; set; }
        [FromForm] public string WidthMobile { get; set; }
        [FromForm] public string HeightMobile { get; set; }
        [FromForm] public bool IsDesktop { get; set; }
        [FromForm] public bool IsMobile { get; set; }
        [FromForm] public int SortOrder { get; set; }
        [FromForm] public string Title { get; set; }
    }
}