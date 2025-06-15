using System;

namespace QLN.Common.DTO_s
{
    public class V2ContentNewsDto
    {
        public Guid Id { get; set; }
        public string Image_url { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string page_name { get; set; }
        public string queue_name { get; set; }
        public string queue_label { get; set; }
        public string node_type { get; set; }
        public string date_created { get; set; }
        public bool IsDeleted { get; set; } = false;
        public string user_name { get; set; }
        public string title { get; set; }
        public string slug { get; set; }
        public string category { get; set; }
        public string category_id { get; set; }
        public string description { get; set; }
    }
}
