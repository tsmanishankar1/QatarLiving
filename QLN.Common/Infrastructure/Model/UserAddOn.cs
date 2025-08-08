using Microsoft.EntityFrameworkCore;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Subscriptions;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static QLN.Common.DTO_s.Enums.Enum;

namespace QLN.Common.Infrastructure.Model
{
    [Index(nameof(ProductCode))]
    [Index(nameof(UserId))]
    [Index(nameof(CompanyId))]
    [Index(nameof(SubscriptionId))]
    [Index(nameof(Vertical))]
    public class UserAddOn
    {
        [Key]
        public Guid UserAddOnId { get; set; }

        [Required]
        [MaxLength(30)]
        public string ProductCode { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? UserId { get; set; }

        public Guid? CompanyId { get; set; }

        public Guid SubscriptionId { get; set; }

        [MaxLength(100)]
        public int? PaymentId { get; set; }

        [Required]
        public SubscriptionVertical Vertical { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public SubscriptionStatus Status { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
