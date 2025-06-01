using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.Model
{
    public class AdBase
    {
        public Guid Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string? L1Category { get; set; }
        public string? L2Category { get; set; }
        public string? Brand { get; set; }
        public string? Condition { get; set; }
        public string? Location { get; set; }
        public string? StreetNumber { get; set; }
        public string? BuildingNumber { get; set; }
        public int? Price { get; set; }
        public string? PhoneNumber { get; set; }
        public string? WhatsappNumber { get; set; }
        public string? ContactEmail { get; set; }
        public string? Warranty { get; set; }
        public string? ImageUpload { get; set; } 
        public DateTime? CreatedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public Guid UserId { get; set; }
        public bool? IsPublished { get; set; }
        public bool? IsPromoted { get; set; }
        public DateTime? RefreshExpiry { get; set; }
        public int RemainingRefreshes { get; set; }
        public int TotalAllowedRefreshes { get; set; }
        public int? Impressions { get; set; }
        public int? Views { get; set; }
        public int? Calls { get; set; }
        public int? WhatsAppClicks { get; set; }
        public int? Shares { get; set; }
        public int? Saves { get; set; }
    }

}
