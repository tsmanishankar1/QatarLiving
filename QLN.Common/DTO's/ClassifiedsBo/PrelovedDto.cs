using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.ClassifiedsBo
{
    public class PrelovedDto
    {
    }

    public class PrelovedViewSubscriptionsDto
    {
        public long AdId { get; set; }
        public int OrderId { get; set; }
        public string? SubscriptionType { get; set; } = null;
        public string? UserName { get; set; } = null;
        public string? Email { get; set; } = null;
        public string? Mobile { get; set; } = null;
        public string? Whatsapp { get; set; } = null;
        public string? WebUrl { get; set; } = null;
        public decimal Amount { get; set; } = 0;
        public string? Status { get; set; } = null;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }      

    }

    public class PreLovedViewP2PDto
    {
        public long AdId { get; set; }
        public int OrderId { get; set; }
        public string? AdType { get; set; } = null;
        public int UserId { get; set; }
        public string? AdTitle { get; set; } = null;
        public string? UserName { get; set; } = null;
        public string? Category { get; set; } = null;
        public string? SubCategory { get; set; } = null;
        public string? Brand { get; set; } = null;
        public DateTime CreatedDate { get; set; }
        public DateTime PublishedDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string? Status { get; set; } = null;
        public string? ImageUrl { get; set; } = null;
        public int Views { get; set; } = 3;
        public int Impressions { get; set; } = 1;
        public int WhatsAppLeads { get; set; } =23;
        public int PhoneLeads { get; set; } = 11;
        public int Share { get; set; } = 5;
        public int Feature { get; set; } = 7;
        public DateTime CreatedDateForApi =>
        CreatedDate == default ? new DateTime(1000, 1, 1) : CreatedDate;
        public DateTime PublishedDateForApi =>
        PublishedDate == default ? new DateTime(1000, 1, 1) : PublishedDate;
        public DateTime ExpiryDateForApi =>
        ExpiryDate == default ? new DateTime(1000, 1, 1) : ExpiryDate;

    }

    public class PreLovedViewP2PTransactionDto
    {
        public long AdId { get; set; }
        public int OrderId { get; set; }
        public string? SubscriptionType { get; set; } = "Preloved 1 Month- P2 Publish";
        public string? UserName { get; set; } = null;
        public string? Email { get; set; } = null;
        public string? Mobile { get; set; } = null;
        public string? Whatsapp { get; set; } = null;
        public decimal Amount { get; set; } = 0;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime PublishedDate { get; set; }
        public int Views { get; set; } = 3;
        public int WhatsAppLeads { get; set; } = 3;
        public int PhoneLeads { get; set; } = 3;
        public string? Status { get; set; } = null;
        public DateTime CreatedDateForApi =>
        CreateDate == default ? new DateTime(1000, 1, 1) : CreateDate;
        public DateTime PublishedDateForApi =>
        PublishedDate == default ? new DateTime(1000, 1, 1) : PublishedDate;

        public DateTime StartDateForApi =>
        StartDate == default ? new DateTime(1000, 1, 1) : StartDate;
        public DateTime EndDateForApi =>
        EndDate == default ? new DateTime(1000, 1, 1) : EndDate;
    }
    public class SubscriptionMockDto
    {
        public Guid SubscriptionId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public Guid CompanyId { get; set; }
        public int PaymentId { get; set; }
        public string Vertical { get; set; } = string.Empty;
        public string SubVertical { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int Status { get; set; }  
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string MetaData { get; set; } = "{}";
    }
    public static class SubscriptionDictionary
    {
        private static readonly Dictionary<string, string> _descriptions = new()
    {
        { "QLC-SUB-6MO", "Preloved 6 months" },
        { "QLC-SUB-1YE", "Preloved 12 months" },
        { "QLC-P2P-PUB", "Preloved - Pay2 Publish - 1 Month" },
        { "QLC-P2P-PRO", "Preloved - Pay2 Promote 1 Month" },
        { "QLC-P2P-FEA", "Preloved- Pay2 Feature 1 Month" },
        { "QLC-ADD-FEA", "Preloved - Add-On Promote 1 Month" },
        { "QLC-ADD-PRO", " Preloved- Add-On Feature 1 Month" }
    };

        public static string GetDescription(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return "Invalid code";

       
            var matchedKey = _descriptions.Keys
                .FirstOrDefault(k => code.StartsWith(k, StringComparison.OrdinalIgnoreCase));

            return matchedKey != null ? _descriptions[matchedKey] : "Unknown subscription code";
        }
    }

    public class BulkEditPreLovedP2PDto
    {
        public List<long> AdIds { get; set; }
        public int AdStatus { get; set; }
    }

}
