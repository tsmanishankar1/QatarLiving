using Microsoft.EntityFrameworkCore;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.Service.TimeSpanConverter;
using QLN.Common.Infrastructure.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
namespace QLN.Common.DTO_s.ClassifiedsBo
{
    public class ClassifiedBOPageResponse<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int? Page { get; set; }
        public int? PerPage { get; set; }
       
    }
    public class XMLFileUploadDto
    {
        public string FileUrl { get; set; }
    }
    public class StoresSubscriptionDto
    {
        [Key]
        public int OrderId { get; set; }
        public string? CompanyId { get; set; } = null;
        public string? SubscriptionId { get; set; } = null;
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
        public int WebLeads { get; set; } = 0;
        public int EmailLeads { get; set; } = 0;
        public int WhatsappLeads { get; set; } = 0;
        public int PhoneLeads { get; set; } = 0;

    }

}
