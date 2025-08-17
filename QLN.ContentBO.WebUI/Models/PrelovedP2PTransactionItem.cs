namespace QLN.ContentBO.WebUI.Models
{
    public class PrelovedP2PTransactionItem
    {
        public long AdId { get; set; }
        public int OrderId { get; set; }
        public string? SubscriptionType { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? Mobile { get; set; }
        public string? Whatsapp { get; set; }
        public decimal Amount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime PublishedDate { get; set; }
        public int Views { get; set; }
        public int WhatsAppLeads { get; set; }
        public int PhoneLeads { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedDateForApi { get; set; }
        public DateTime PublishedDateForApi { get; set; }
        public DateTime StartDateForApi { get; set; }
        public DateTime EndDateForApi { get; set; }
    }
}
