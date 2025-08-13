namespace QLN.ContentBO.WebUI.Models
{
    public class ServiceAdPaymentSummaryDto
    {
        public long AddId { get; set; }
        public string? AddTitle { get; set; }
        public string? UserName { get; set; }
        public string? EmailAddress { get; set; }
        public string? Mobile { get; set; }
        public string? SubscriptionPlan { get; set; }
        public string? WhatsappNumber { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
        public ServiceStatus? Status { get; set; }
        public string? OrderId { get; set; }
        public decimal? Amount { get; set; }
    }
    public class PaginatedPaymentSummaryResponse
    {
        public List<ServiceAdPaymentSummaryDto> items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PerSize { get; set; }
    }
}