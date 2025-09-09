namespace QLN.ContentBO.WebUI.Models
{
    public class DealsSubscriptionResponse
    {
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public List<DealsSubscriptionItem> Items { get; set; } = [];
    }

    public class DealsSubscriptionItem
    {
        public long AdId { get; set; }
        public int OrderId { get; set; }
        public string SubscriptionType { get; set; }
        public string Status { get; set; }
        public string Price { get; set; }
        public string Email { get; set; }
        public string CreatedBy { get; set; }
        public string ContactNumber { get; set; }
        public string WhatsappNumber { get; set; }
        public string WhatsAppLeads { get; set; }
        public string PhoneLeads { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string UserName { get; set; }
    }
}
