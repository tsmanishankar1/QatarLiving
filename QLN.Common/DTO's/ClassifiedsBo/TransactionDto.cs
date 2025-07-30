using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.ClassifiedsBoIndex
{
    public class TransactionDto
    {
        public string Id { get; set; } = string.Empty;
        public string AdId { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string TransactionType { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string Whatsapp { get; set; } = string.Empty;
        public string Account { get; set; } = string.Empty;
        public string CreationDate { get; set; } = string.Empty;
        public string PublishedDate { get; set; } = string.Empty;
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class TransactionListResponseDto
    {
        public List<TransactionDto> Records { get; set; } = new();
        public int TotalRecords { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}
