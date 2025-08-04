using System.ComponentModel.DataAnnotations;
namespace QLN.ContentBO.WebUI.Models
{
    public class ServiceSubscriptionAdSummaryDto
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
        public string? Favorites { get; set; }
        public List<ImageDto>? ImageUpload { get; set; }
        public string? OrderId { get; set; }

    }
    public class PaginatedSubscriptionAdResponse
    {
        public List<ServiceSubscriptionAdSummaryDto> items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PerSize { get; set; }
    }
}