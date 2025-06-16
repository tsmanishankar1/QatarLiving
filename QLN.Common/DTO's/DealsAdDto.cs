using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class DealsAdDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string SubVertical { get; set; }
        public string FlyerFile { get; set; }
        public List<string> ImageUrl { get; set; }
        public string XMLLink { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string PhoneNumber { get; set; }
        public string WhatsAppNumber { get; set; }
        public List<string> Location { get; set; }
        public Guid UserId { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsPromoted { get; set; }
        public DateTime CreatedAt { get; set; }
        public AdStatus Status { get; set; }
    }

    public class DealsAdListDto
    {
        public List<DealsAdDto> PublishedAds { get; set; } = new();
        public List<DealsAdDto> UnpublishedAds { get; set; } = new();
    }
}
