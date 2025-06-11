using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class ItemsAdCreatedResponseDto
    {
        public Guid AdId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string CertificateUrl { get; set; }
        public List<string> ImageUrls { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }
}
