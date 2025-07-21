using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class V2ClassifiedLandingBoDto
    {
        public string Id { get; set; }
        public string EntityType { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int SlotOrder { get; set; }
        public bool IsActive { get; set; }
        public string ImageUrl { get; set; }
        public int ListingCount { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PerPage { get; set; }
        public int FeaturedCount { get; set; }
        public int FeaturedInCurrentPage { get; set; }
    }
    public class L1CategoryDto
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = default!;
        public List<CategoryField>? Fields { get; init; }
        public string Vertical { get; init; } = default!;
        public DateTime? ExpiryDate { get; set; }
    }


}
