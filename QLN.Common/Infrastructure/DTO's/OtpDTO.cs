using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.DTO_s
{
    public class OtpDTO
    {
        public class EmailOtpRequest
        {
            [Required]
            public string Email { get; set; } = string.Empty;
        }

        public class VerifyEmailOtpRequest
        {
            [Required]
            public string Email { get; set; } = string.Empty;
            [Required]
            public string Otp { get; set; } = string.Empty;
        }

        public class PhoneOtpRequest
        {
            [Required]
            public string PhoneNumber { get; set; } = string.Empty;
        }

        public class VerifyPhoneOtpRequest
        {
            [Required]
            public string PhoneNumber { get; set; } = string.Empty;
            [Required]
            public string Otp { get; set; } = string.Empty;
        }
    }
}
