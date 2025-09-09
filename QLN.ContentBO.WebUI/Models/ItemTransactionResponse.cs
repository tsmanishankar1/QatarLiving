namespace QLN.ContentBO.WebUI.Models
{
    public class ItemTransactionResponse
    {
        public List<ItemTransactionItem> Records { get; set; }
        public int TotalRecords { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class ItemTransactionItem
    {
        public string Id { get; set; }
        public string AdId { get; set; }
        public int PaymentId { get; set; }
        public int OrderId { get; set; }
        public string UserId { get; set; }
        public string Username { get; set; }
        public string UserEmail { get; set; }
        public string TransactionType { get; set; }
        public int ProductType { get; set; }
        public string Category { get; set; }
        public string Status { get; set; }
        public string Email { get; set; }
        public string Mobile { get; set; }
        public string Whatsapp { get; set; }
        public string Account { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime PublishedDate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Amount { get; set; }
        public int PaymentMethod { get; set; }
        public string Description { get; set; }
    }
}
