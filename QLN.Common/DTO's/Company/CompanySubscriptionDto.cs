using QLN.Common.Infrastructure.Subscriptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.Company
{
    public class CompanySubscriptionDto
    {
        public Guid? CompanyId { get; set; }
        public string CompanyName { get; set; }
        public string Email { get; set; }
        public string Mobile { get; set; }
        public string WhatsApp { get; set; }
        public string WebUrl { get; set; }
        public SubscriptionStatus Status { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Slug { get; set; }
        [JsonIgnore]
        public string? SubscriptionType { get; set; }
    }
    public class CompanySubscriptionFilter
    {
        public string? SubscriptionType { get; set; }
        public DateTime? StartDate { get; set; } 
        public DateTime? EndDate { get; set; }
        public string? SearchTerm { get; set; } 
        public string? SortBy { get; set; }
        public int? PageNumber { get; set; }
        public int? PageSize { get; set; }
    }
    public class CompanySubscriptionListResponseDto
    {
        public List<CompanySubscriptionDto> Records { get; set; }
        public int TotalRecords { get; set; }
        public int? PageNumber { get; set; }
        public int? PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}
