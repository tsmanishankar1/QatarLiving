namespace QLN.ContentBO.WebUI.Models
{
    public class StoreSubscriptionResponse
    {
        public List<StoreSubscriptionItem> Items { get; set; } = [];
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PerPage { get; set; }
    }

    public class StoreSubscriptionItem
    {
        public Guid? CompanyId { get; set; }
        public int OrderId { get; set; }
        public string SubscriptionType { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string Whatsapp { get; set; } = string.Empty;
        public string WebUrl { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int WebLeads { get; set; }
        public int EmailLeads { get; set; }
        public int WhatsappLeads { get; set; }
        public int PhoneLeads { get; set; }
    }
}
