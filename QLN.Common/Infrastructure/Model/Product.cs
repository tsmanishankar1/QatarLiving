using Microsoft.EntityFrameworkCore;
using QLN.Common.DTO_s;
using QLN.Common.DTO_s.Payments;
using QLN.Common.DTO_s.Subscription;
using QLN.Common.Infrastructure.Subscriptions;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLN.Common.Infrastructure.Model
{
    [Index(nameof(ProductCode), IsUnique = true)]
    [Index(nameof(Vertical))]
    public class Product
    {
        [Key]
        [MaxLength(30)]
        public string ProductCode { get; set; } = string.Empty; 

        [Required]
        [MaxLength(20)]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        public ProductType ProductType { get; set; }

        [Required]
        public Vertical Vertical { get; set; }

        public Vertical? ParentVertical { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Required]
        [MaxLength(10)]
        public string Currency { get; set; } = "QAR";

        public ProductConstraints? Constraints { get; set; } = new();

        public bool IsActive { get; set; } = true;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
