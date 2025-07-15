using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class ServicesDto
    {
        public Guid Id { get; set; }
        public Guid MainCategoryId { get; set; }
        public Guid L1CategoryId { get; set; }
        public Guid L2CategoryId { get; set; }
        public string? Price { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string PhoneNumber { get; set; }
        public string WhatsappNumber { get; set; }
        public string? EmailAddress { get; set; }
        public string Location { get; set; }
        public List<ImageDto>? PhotoUpload { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
    public class ImageDto
    {
        public string? FileName { get; set; }
        public string? Url { get; set; }
        public int Order { get; set; }
    }
}
