using QLN.Common.Infrastructure.Subscriptions;
using QLN.Common.Infrastructure.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.ClassifiedsBo
{
    public class FeaturedStoreDto
    {
        public string Title { get; set; } = null!;
        public Vertical Vertical { get; set; }
        public string StoreId { get; set; } = null!;
        public string StoreName { get; set; } = null!;
        public string ImageUrl { get; set; } = null!;
        [JsonConverter(typeof(FlexibleDateOnlyConverter))]
        public DateOnly StartDate { get; set; }
        [JsonConverter(typeof(FlexibleDateOnlyConverter))]
        public DateOnly EndDate { get; set; }
    }

    public class EditFeaturedStoreDto
    {
        public string Id { get; set; } = null!;
        public string Title { get; set; } = null!;
        public Vertical Vertical { get; set; }
        public string StoreId { get; set; } = null!;
        public string StoreName { get; set; } = null!;
        public string ImageUrl { get; set; } = null!;
        [JsonConverter(typeof(FlexibleDateOnlyConverter))]
        public DateOnly StartDate { get; set; }
        [JsonConverter(typeof(FlexibleDateOnlyConverter))]
        public DateOnly EndDate { get; set; }
        public int SlotOrder { get; set; }
    }
}
