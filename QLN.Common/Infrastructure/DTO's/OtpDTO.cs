using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.DTO_s
{
    public class OtpDTO
    {
        public class EmailOtpRequest
        {
            public string Email { get; set; } = string.Empty;
        }

        public class VerifyEmailOtpRequest
        {
            public string Email { get; set; } = string.Empty;
            public string Otp { get; set; } = string.Empty;
        }

        public class PhoneOtpRequest
        {
            public string PhoneNumber { get; set; } = string.Empty;
        }

        public class VerifyPhoneOtpRequest
        {
            public string PhoneNumber { get; set; } = string.Empty;
            public string Otp { get; set; } = string.Empty;
        }
    }
}
