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
        public string Id { get; set; } 
        public string AdId { get; set; }
        public int PaymentId { get; set; }
        public int OrderId { get; set; } 
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string TransactionType { get; set; } = string.Empty;
        public int ProductType { get; set; } 
        public string Category { get; set; } = string.Empty;
        public string Status { get; set; } 
        public string Email { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string Whatsapp { get; set; } = string.Empty;
        public string Account { get; set; } = string.Empty;
        public DateTime CreationDate { get; set; } 
        public DateTime? PublishedDate { get; set; } 
        public DateTime StartDate { get; set; } 
        public DateTime EndDate { get; set; } 
        public decimal Amount { get; set; }
        public int? PaymentMethod { get; set; } 
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
