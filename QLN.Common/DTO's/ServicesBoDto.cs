using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class ServiceAdSummaryDto
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? CategoryName { get; set; }
        public string? L1CategoryName { get; set; }
        public ServiceStatus? Status { get; set; } 
        public DateTime CreatedAt { get; set; }
        public DateTime? PublishedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public List<ImageDto>? PhotoUpload { get; set; }
        public Guid? PaymentTransactionId { get; set; }
    }
    
}
