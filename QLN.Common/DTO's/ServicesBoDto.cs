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
        public string AdTitle { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? SubCategory { get; set; }
        public ServiceStatus? Status { get; set; } 
        public bool? IsPromoted { get; set; }
        public bool? IsFeatured { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime? DatePublished { get; set; }
        public DateTime? DateExpiry { get; set; }
        public List<ImageDto>? ImageUpload { get; set; }
        public Guid? PaymentTransactionId { get; set; }
    }
    public class ServiceAdQueryParams
    {
        public string? SortBy { get; set; }
        public string? Search { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public DateTime? PublishedFrom { get; set; }
        public DateTime? PublishedTo { get; set; }
    }
    public class PaginatedResult<T>
    {
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public List<T> Items { get; set; } = new();
    }

}
