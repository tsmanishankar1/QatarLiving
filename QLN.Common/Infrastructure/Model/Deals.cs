using QLN.Common.DTO_s;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.Model
{
    public class Deals
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [MaxLength(50)]
        public string? SubscriptionId { get; set; }

        [Required]
        [MaxLength(100)]
        public string UserId { get; set; } = string.Empty;

        [MaxLength(1500)]
        public string? BusinessName { get; set; }
        [MaxLength(1000)]
        public string? BranchNames { get; set; }

        [MaxLength(100)]
        public string? BusinessType { get; set; }

        [MaxLength(300)]
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        [Required]
        [MaxLength(100)]
        public string FlyerFileUrl { get; set; } = string.Empty;
        [MaxLength(100)]
        public string? DataFeedUrl { get; set; }

        [Required]
        [MaxLength(20)]
        public string ContactNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string WhatsappNumber { get; set; } = string.Empty;
        [MaxLength(100)]
        public string? WebsiteUrl { get; set; }

        [MaxLength(100)]
        public string? SocialMediaLinks { get; set; }

        public bool IsActive { get; set; }

        [Column(TypeName = "jsonb")]
        public LocationsDtos Locations { get; set; }

        [Required]
        [MaxLength(100)]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        [MaxLength(100)]
        public string? UpdatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }

        [Required]
        [MaxLength(100)]
        public string XMLlink { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Offertitle { get; set; }

        [Column(TypeName = "jsonb")]
        public List<ImageInfo> Images { get; set; } = new List<ImageInfo>();
        public DateTime ExpiryDate { get; set; }
        public DateTime? FeaturedExpiryDate { get; set; }
        public DateTime? PromotedExpiryDate { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsPromoted { get; set; }
        [Required]
        public AdStatus Status { get; set; }
    }
}
