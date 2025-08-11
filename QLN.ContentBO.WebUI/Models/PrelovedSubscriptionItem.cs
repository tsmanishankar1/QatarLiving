namespace QLN.ContentBO.WebUI.Models
{
    public class PrelovedSubscriptionItem
    {
        public int AdId { get; set; }
        public int OrderId { get; set; }
        public string? SubscriptionType { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? Mobile { get; set; }
        public string? Whatsapp { get; set; }
        public string? WebUrl { get; set; }
        public decimal Amount { get; set; }
        public string? Status { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
