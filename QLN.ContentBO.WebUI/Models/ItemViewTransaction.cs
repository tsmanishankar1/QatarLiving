using System;

namespace QLN.ContentBO.WebUI.Models
{
     public class PagedTransactionResponse
    {
        public List<ItemViewTransaction> Records { get; set; }
        public int TotalRecords { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
    public class ItemViewTransaction
    {
        public string Id { get; set; }
        public string AdId { get; set; }
        public string OrderId { get; set; }
        public string UserId { get; set; }
        public string Username { get; set; }
        public string UserEmail { get; set; }
        public string TransactionType { get; set; }
        public string ProductType { get; set; }
        public string Category { get; set; }
        public string Status { get; set; }
        public string Email { get; set; }
        public string Mobile { get; set; }
        public string Whatsapp { get; set; }
        public string Account { get; set; }
        public string CreationDate { get; set; } // You can change this to DateTime if needed
        public string PublishedDate { get; set; } // Same here
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; }
        public string Description { get; set; }
    }
}
