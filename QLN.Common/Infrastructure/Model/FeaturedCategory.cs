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
    public class FeaturedCategory
    {
        public Guid Id { get; set; }
        public Vertical Vertical { get; set; }
        [MaxLength(100)]
        public string Title { get; set; } = null!;
        [MaxLength(100)]
        public string CategoryName { get; set; } = null!;
        public long CategoryId { get; set; }
        [MaxLength(100)]
        public string L1categoryName { get; set; } = null!;
        public long L1CategoryId { get; set; }
        public bool IsActive { get; set; }
        [JsonConverter(typeof(FlexibleDateOnlyConverter))]
        public DateOnly StartDate { get; set; }
        [JsonConverter(typeof(FlexibleDateOnlyConverter))]
        public DateOnly EndDate { get; set; }
        public int SlotOrder { get; set; }
        [MaxLength(100)]
        public string ImageUrl { get; set; } = null!;
        [MaxLength(200)]
        public string? Slug { get; set; }
        [MaxLength(50)]
        public string CreatedBy { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        [MaxLength(50)]
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
