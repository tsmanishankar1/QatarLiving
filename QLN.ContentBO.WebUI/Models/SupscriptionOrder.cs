namespace QLN.ContentBO.WebUI.Models
{
    public class SubscriptionOrder
    {
        public int AdId { get; set; }
        public int OrderId { get; set; }
        public string SubscriptionPlan { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string Whatsapp { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string ImageUrl { get; set; }
    }
}




