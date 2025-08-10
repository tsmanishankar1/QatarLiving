namespace QLN.ContentBO.WebUI.Models
{
    public class SubscriptionListingModal
    {
        public int Id { get; set; }
        public string AdId { get; set; }
        public string OrderId { get; set; }
        public int UserId { get; set; }
        public int SubscriptionId { get; set; }
        public string SubscriptionType { get; set; }
        public string AdTitle { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Mobile { get; set; }
        public string Whatsapp { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime PublishedDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public int WhatsAppCount { get; set; }
        public int PhoneCount {  get; set; }
    }
    public class PrelovedSubscriptionResponse
    {
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public List<PrelovedSubscriptionItem> Items { get; set; } = new();
    }

    public class PrelovedSubscriptionItem
    {
        public string AdId { get; set; }
        public string OrderId { get; set; }
        public string SubscriptionType { get; set; }
        public string UserName { get; set; }
        public string EmailAddress { get; set; }
        public string Mobile { get; set; }
        public string WhatsappNumber { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public int WhatsAppCount { get; set; }
        public int PhoneCount { get; set; }
    }

}
