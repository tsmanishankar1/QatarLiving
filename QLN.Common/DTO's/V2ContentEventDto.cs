using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class V2ContentEventDto
    {
        public Guid Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string? UserName { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public EventCategory Category { get; set; }
        public string? CategoryId { get; set; }
        public string? EventVenue { get; set; }
        public DateTime EventStart { get; set; }
        public DateTime EventEnd { get; set; }
        public string? EventLat { get; set; }
        public string? EventLong { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? EntityOrganizer { get; set; }
        public string? EventLocation { get; set; }
        public bool IsActive { get; set; } = true;
        public Guid CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
