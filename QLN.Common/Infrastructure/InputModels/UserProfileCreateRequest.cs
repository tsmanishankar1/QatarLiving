using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.InputModels
{
    public class UserProfileCreateRequest
    {
        public string Firstname { get; set; } = null!;
        public string Lastname { get; set; } = null!;
        public DateTime Dateofbirth { get; set; }
        public string Gender { get; set; } = null!;
        public string Mobilenumber { get; set; } = null!;
        public string Emailaddress { get; set; } = null!;
        public string Nationality { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string Confirmpassword { get; set; } = null!;
        public string? Languagepreferences { get; set; }
        public string? Location { get; set; }
        public int Createdby { get; set; }
    }
    public class EmailRequest
    {
        public string Email { get; set; } = null!;
    }
    public class OtpVerificationRequest
    {
        public string Otp { get; set; } = null!;
    }

}
