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
    public class SeasonalPicksDto
    {
        public string Title { get; set; } = null!;
        public Vertical Vertical { get; set; }
        public long CategoryId { get; set; }
        public string CategoryName { get; set; } = null!;
        public long L1CategoryId { get; set; }
        public string L1categoryName { get; set; } = null!;
        public long L2categoryId { get; set; }
        public string L2categoryName { get; set; } = null!;
        [JsonConverter(typeof(FlexibleDateOnlyConverter))]
        public DateOnly StartDate { get; set; }
        [JsonConverter(typeof(FlexibleDateOnlyConverter))]
        public DateOnly EndDate { get; set; }
        public string ImageUrl { get; set; } = null!;
    }

    public class EditSeasonalPickDto
    {
        public string Id { get; set; } = null!;
        public string Title { get; set; } = null!;
        public Vertical Vertical { get; set; }
        public long CategoryId { get; set; }
        public string CategoryName { get; set; } = null!;
        public long L1CategoryId { get; set; }
        public string L1categoryName { get; set; } = null!;
        public long L2categoryId { get; set; }
        public string L2categoryName { get; set; } = null!;
        [JsonConverter(typeof(FlexibleDateOnlyConverter))]
        public DateOnly StartDate { get; set; }
        [JsonConverter(typeof(FlexibleDateOnlyConverter))]
        public DateOnly EndDate { get; set; }
        public string ImageUrl { get; set; } = null!;
        public int SlotOrder { get; set; }
    }
}
