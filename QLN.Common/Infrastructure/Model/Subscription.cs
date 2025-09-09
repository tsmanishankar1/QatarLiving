using Microsoft.EntityFrameworkCore;
using QLN.Common.DTO_s;
using QLN.Common.DTO_s.Payments;
using QLN.Common.DTO_s.Subscription;
using QLN.Common.Infrastructure.Subscriptions;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLN.Common.Infrastructure.Model
{
    [Index(nameof(ProductCode))]
    [Index(nameof(UserId))]
    [Index(nameof(CompanyId))]
    [Index(nameof(Vertical))]
    public class Subscription
    {
        [Key]
        public Guid SubscriptionId { get; set; }

        [Required]
        [MaxLength(50)]
        public string ProductCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string ProductName { get; set; } = string.Empty;

        public ProductType? ProductType { get; set; }

        [MaxLength(100)]
        public string? UserId { get; set; }

        public Guid? CompanyId { get; set; }

        public int? PaymentId { get; set; }

        public long? AdId { get; set; }

        [Required]
        public Vertical Vertical { get; set; }

        public SubVertical? SubVertical { get; set; }

        public SubscriptionQuota Quota { get; set; } = new();

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public SubscriptionStatus Status { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [ForeignKey(nameof(ProductCode))]
        public virtual Product Product { get; set; } = null!;

        public virtual ICollection<UserAddOn> UserAddOns { get; set; } = new List<UserAddOn>();
    }
}