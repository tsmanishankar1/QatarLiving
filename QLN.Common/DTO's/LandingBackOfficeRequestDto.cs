using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class LandingBackOfficeRequestDto
    {
        public string Title { get; set; } = default!;

        public string? Description { get; set; }

        public int? Order { get; set; }

        public string? ParentId { get; set; }

        public bool IsActive { get; set; } = true;

        public string? RediectUrl { get; set; }

        public string? ImageUrl { get; set; }

        public int? ListingCount { get; set; }

        public int? RotationSeconds { get; set; }
        public string? EntityId { get; set; }

        public CommonSearchRequest? PayloadJson { get; set; }
    }
}
