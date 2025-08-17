using QLN.Common.Infrastructure.Subscriptions;
using QLN.Common.Infrastructure.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.Model
{
    public class FeaturedStore
    {
        public Guid Id { get; set; }
        [MaxLength(100)]
        public string Title { get; set; } = null!;
        public Vertical Vertical { get; set; }
        [MaxLength(50)]
        public string StoreId { get; set; } = null!;
        [MaxLength(100)]
        public string StoreName { get; set; } = null!;
        [MaxLength(200)]
        public string? Slug { get; set; }
        [MaxLength(100)]
        public string ImageUrl { get; set; } = null!;
        [JsonConverter(typeof(FlexibleDateOnlyConverter))]
        public DateOnly StartDate { get; set; }
        [JsonConverter(typeof(FlexibleDateOnlyConverter))]
        public DateOnly EndDate { get; set; }
        public int SlotOrder { get; set; }
        public bool IsActive { get; set; }
        [MaxLength(50)]
        public string CreatedBy { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        [MaxLength(50)]
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
    public class FeaturedStoreItem
    {
        public Guid Id { get; set; }
        [MaxLength(100)]
        public string Title { get; set; } = null!;
        public Vertical Vertical { get; set; }
        [MaxLength(50)]
        public string StoreId { get; set; } = null!;
        [MaxLength(100)]
        public string StoreName { get; set; } = null!;
        [MaxLength(100)]
        public string ImageUrl { get; set; } = null!;
        [JsonConverter(typeof(FlexibleDateOnlyConverter))]
        public DateOnly StartDate { get; set; }
        [JsonConverter(typeof(FlexibleDateOnlyConverter))]
        public DateOnly EndDate { get; set; }
        public int SlotOrder { get; set; }
        public bool IsActive { get; set; }
        [MaxLength(50)]
        public string CreatedBy { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        [MaxLength(50)]
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public int ProductCount { get; set; } = 0;
    }
}
