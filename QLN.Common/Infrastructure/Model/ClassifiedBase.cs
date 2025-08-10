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
    public class ClassifiedBase
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public SubVertical SubVertical { get; set; }
        public AdTypeEnum AdType { get; set; }
        [Required, MaxLength(255)]
        public string Title { get; set; } = string.Empty;
        [MaxLength(255)]
        public string? Description { get; set; }
        public double? Price { get; set; }
        [MaxLength(50)]
        public string? PriceType { get; set; }

        public long? CategoryId { get; set; }
        [MaxLength(100)]
        public string? Category { get; set; }
        public long? L1CategoryId { get; set; }
        [MaxLength(100)]
        public string? L1Category { get; set; }
        public long? L2CategoryId { get; set; }
        [MaxLength(100)]
        public string? L2Category { get; set; }
        [MaxLength(100)]
        public string? Location { get; set; }
        [MaxLength(100)]
        public string? Brand { get; set; }
        [MaxLength(100)]
        public string? Model { get; set; }
        [MaxLength(100)]
        public string? Condition { get; set; }
        [MaxLength(100)]
        public string? Color { get; set; }
        public DateTime? PublishedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public AdStatus Status { get; set; }
        [Required, MaxLength(100)]
        public string UserId { get; set; } = string.Empty;
        [Required, MaxLength(100)]
        public string UserName { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        [MaxLength(10)]
        public string ContactNumberCountryCode { get; set; } = string.Empty;
        [MaxLength(20)]
        public string ContactNumber { get; set; } = string.Empty;
        [MaxLength(100)]
        public string ContactEmail { get; set; } = string.Empty;
        [MaxLength(10)]
        public string WhatsappNumberCountryCode { get; set; } = string.Empty;
        [MaxLength(10)]
        public string WhatsAppNumber { get; set; } = string.Empty;
        [MaxLength(100)]
        public string? StreetNumber { get; set; }
        [MaxLength(100)]
        public string? BuildingNumber { get; set; }
        [MaxLength(50)]
        public string zone { get; set; }
        [Column(TypeName = "jsonb")]
        public List<ImageInfo> Images { get; set; } = new List<ImageInfo>();
        [Column(TypeName = "jsonb")]
        public Dictionary<string, string>? Attributes { get; set; }
        public bool IsActive { get; set; } = true;
        [MaxLength(100)]
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        [MaxLength(100)]
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid? SubscriptionId { get; set; }
    }
}
