using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class ContentEventDto
    {
        public Guid id { get; set; }
        public string image_url { get; set; }
        public string author_name { get; set; }
        public string title { get; set; }
        public string slug { get; set; }
        public EventCategory category { get; set; }
        public string category_id { get; set; }
        public string event_venue { get; set; }
        public string event_start { get; set; }
        public string event_end { get; set; }
        public string event_lat { get; set; }
        public string event_long { get; set; }
        public string description { get; set; }
        public string entity_organizer { get; set; }
        public string event_location { get; set; }
        public bool IsActive { get; set; } = true;
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
