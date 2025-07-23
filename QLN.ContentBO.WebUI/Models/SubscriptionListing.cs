namespace QLN.ContentBO.WebUI.Models
{
    public class SubscriptionListing
    {
        public int AdId { get; set; }
        public int UserId { get; set; }
        public string AdTitle { get; set; }
        public int InternalUserId { get; set; }
        public string UserName { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public string Section { get; set; }
        public string Email { get; set; }
        public string Mobile { get; set; }
        public string Whatsapp { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime PublishedDate { get; set; }
        public DateTime ExpiryDate { get; set; }
    }
}
