using QLN.Common.Infrastructure.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.ClassifiedsBo
{
    public class FeaturedCategoryDto
    {
        public string Vertical { get; set; } = null!;
        public string CategoryName { get; set; } = null!;
        public string CategoryId { get; set; } = null!;
        [JsonConverter(typeof(FlexibleDateOnlyConverter))]
        public DateOnly StartDate { get; set; }
        [JsonConverter(typeof(FlexibleDateOnlyConverter))]
        public DateOnly EndDate { get; set; }
        public string ImageUrl { get; set; }
    }
}
