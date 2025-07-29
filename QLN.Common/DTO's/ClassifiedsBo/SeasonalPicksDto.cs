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
        public string Vertical { get; set; }
        public string CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string L1CategoryId { get; set; }
        public string L1categoryName { get; set; }
        public string L2categoryId { get; set; }
        public string L2categoryName { get; set; }
        [JsonConverter(typeof(FlexibleDateOnlyConverter))]
        public DateOnly StartDate { get; set; }
        [JsonConverter(typeof(FlexibleDateOnlyConverter))]
        public DateOnly EndDate { get; set; }
        public string ImageUrl { get; set; }
    }
}
