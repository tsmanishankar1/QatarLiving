namespace QLN.ContentBO.WebUI.Models
{
  public class ServiceP2PAdSummaryDto
  {
    public long Id { get; set; }
    public string ProductType { get; set; } = string.Empty;
    public string AdTitle { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public string Whatsapp { get; set; } = string.Empty;
    public string Amount { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public string Views { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public ServiceStatus? Status { get; set; }
    public DateTime CreationDate { get; set; }
    public DateTime? DatePublished { get; set; }
    public string? OrderId { get; set; }
  }
  public class PaginatedP2PResponse
    {
        public List<ServiceP2PAdSummaryDto> items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PerSize { get; set; }
    }
}