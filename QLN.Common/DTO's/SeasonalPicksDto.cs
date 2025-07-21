using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class SeasonalPicksDto
    {
        public string Vertical { get; set; }
        public Guid Id { get; set; }
        public string CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string L1CategoryId { get; set; }
        public string L1categoryName { get; set; }
        public string L2categoryId { get; set; }
        public string L2categoryName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string ImageUrl { get; set; }
        public int? SlotOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
