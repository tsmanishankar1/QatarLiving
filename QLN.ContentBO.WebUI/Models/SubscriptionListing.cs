namespace QLN.ContentBO.WebUI.Models
{
    public class SubscriptionListing
    {
        public int Id { get; set; }
        public int AdId { get; set; }
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
    }
}
