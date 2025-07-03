using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class V2CommunityPostDto
    {
        public Guid Id { get; set; }
        public string? UserName { get; set; }
        public string Title { get; set; }
        public string? Slug { get; set; }
        public string? Category { get; set; }
        public string? CategoryId { get; set; }
        public string? Description { get; set; }
        public string ?ImageUrl { get; set; } 
        public string? ImageBase64 { get; set; } 
        public bool? IsActive { get; set; } = true;
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public DateTime? DateCreated { get; set; }
    }

}
