using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class ServiceAdSummaryDto
    {
        public long Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string AdTitle { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? SubCategory { get; set; }
        public ServiceStatus? Status { get; set; } 
        public string Certificate { get; set; }
        public bool? IsPromoted { get; set; }
        public bool? IsFeatured { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? DatePublished { get; set; }
        public DateTime? DateExpiry { get; set; }
        public List<ImageDto>? ImageUpload { get; set; }
        public string? OrderId { get; set; }
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
    public class ServiceAdPaymentSummaryDto
    {
        public long AddId { get; set; }
        public string? AddTitle { get; set; }
        public string? UserName { get; set; }
        public string? EmailAddress { get; set; }
        public string? Mobile{ get;set; }
        public string? SubscriptionPlan { get; set; }
        public string? WhatsappNumber { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public ServiceStatus? Status { get; set; }
        public string? OrderId { get; set; }
        public decimal? Amount { get; set; }
        public DateTime CreatedAt { get; set; }

    }
    public class ServiceP2PAdSummaryDto
    {
        public long Id { get; set; }
       public string ProductType { get; set; } = string.Empty;
        public string AdTitle { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string Whatsapp { get; set; } = string.Empty;
        public decimal Amount { get; set; } 
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public string Views { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
       
        public ServiceStatus? Status { get; set; }
       
        public DateTime CreatedAt{ get; set; }
        public DateTime? DatePublished { get; set; }
     
        public string? OrderId { get; set; }
    }
    public class PaginationQuery
    {
        public int? PageNumber { get; set; } = 1;
        public int? PageSize { get; set; } = 12;
    }
    public class ServiceSubscriptionAdSummaryDto
    {
        public long Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string AdTitle { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? SubCategory { get; set; }
        public ServiceStatus? Status { get; set; }
        public bool? IsPromoted { get; set; }
        public bool? IsFeatured { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? DatePublished { get; set; }
        public DateTime? DateExpiry { get; set; }
        public string? Favorites { get; set; }
        public List<ImageDto>? ImageUpload { get; set; }
        public string? OrderId { get; set; }
    }

}
