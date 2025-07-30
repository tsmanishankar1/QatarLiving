using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.ClassifiedsBo
{
    public class PrelovedAdPaymentSummaryDto
    {
        public Guid AdId { get; set; }
        public string? OrderId { get; set; }
        public string? SubscriptionType { get; set; }
        public string? UserName { get; set; }
        public string? EmailAddress { get; set; }
        public string? Mobile { get; set; }
        public string? WhatsappNumber { get; set; }
        public double? Amount { get; set; }
        public string? Status { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
    }
}
