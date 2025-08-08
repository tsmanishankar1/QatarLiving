using QLN.Common.DTO_s.Payments;
using QLN.Common.Infrastructure.Subscriptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static QLN.Common.DTO_s.Enums.Enum;

namespace QLN.Common.DTO_s.Subscription
{
    public class CreateProductDto
    {
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public ProductType ProductType { get; set; }
        public SubscriptionVertical Vertical { get; set; }
        public SubscriptionVertical? ParentVertical { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; } = "QAR";
        public ProductConstraints? Constraints { get; set; }
    }
    public class UpdateProductDto
    {
        public string? ProductName { get; set; }
        public decimal? Price { get; set; }
        public string? Currency { get; set; }
        public ProductConstraints? Constraints { get; set; }
        public bool? IsActive { get; set; }
    }
    public class ProductResponseDto
    {
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public ProductType ProductType { get; set; }
        public SubscriptionVertical Vertical { get; set; }
        public SubscriptionVertical? ParentVertical { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; } = string.Empty;
        public ProductConstraints Constraints { get; set; } = new();
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
