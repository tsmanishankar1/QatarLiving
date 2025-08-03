namespace QLN.ContentBO.WebUI.Models
{
    public class PreLovedTransactionModal
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
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Views { get; set; }
        public string MobileCount { get; set; }
        public string WhatsappCount { get; set; }
    }
    public class PrelovedTransactionResponse
    {
        public int TotalCount { get; set; }
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
        public List<PrelovedTransactionItem> Records { get; set; } = new();
    }

    public class PrelovedTransactionItem
    {
        public string AdId { get; set; }
        public string OrderId { get; set; }
        public string SubscriptionType { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Mobile { get; set; }
        public string Whatsapp  { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string CreationDate { get; set; }
        public string PublishedDate { get; set; }
        public int Views { get; set; }  
        public int MobileCount { get; set; } 
        public int WhatsappCount { get; set; }

    }

}
