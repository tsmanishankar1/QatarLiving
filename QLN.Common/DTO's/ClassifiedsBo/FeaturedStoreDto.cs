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
        public string Vertical { get; set; }
        public string StoreId { get; set; }
        public string StoreName { get; set; }
        public string ImageUrl { get; set; }
        [JsonConverter(typeof(FlexibleDateOnlyConverter))]
        public DateOnly StartDate { get; set; }
        [JsonConverter(typeof(FlexibleDateOnlyConverter))]
        public DateOnly EndDate { get; set; }

    }
}
