using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.ClassifiedsBo
{
    public class PrelovedTransactionDto
    {
        public string Id { get; set; } = string.Empty;
        public string AdId { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public string SubscriptionType { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string Whatsapp { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string CreationDate { get; set; } = string.Empty;
        public string PublishedDate { get; set; } = string.Empty;
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public int Views { get; set; }
        public int MobileCount { get; set; }
        public int WhatsappCount { get; set; }
    }

    public class PrelovedTransactionListResponseDto
    {
        public List<PrelovedTransactionDto> Records { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

}
