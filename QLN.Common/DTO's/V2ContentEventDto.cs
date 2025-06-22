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
        public string Image_url { get; set; } = string.Empty;
        public Guid User_id { get; set; }
        public string? User_name { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public string Category { get; set; } = "music";
        public string? Category_id { get; set; }
        public string? Event_venue { get; set; }
        public DateTime Event_start { get; set; }
        public DateTime Event_end { get; set; }
        public string? Event_lat { get; set; }
        public string? Event_long { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? Entity_organizer { get; set; }
        public string? Event_location { get; set; }
        public bool IsActive { get; set; } = true;
        public Guid CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
