using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.Payments
{
    public class FatoraVerificationResponse
    {
        public string Status { get; set; } // "SUCCESS" or "ERROR"
        public PaymentVerificationSuccessResponse? Result { get; set; }
        public PaymentVerificationFailureResponse? Error { get; set; }
    }
}
