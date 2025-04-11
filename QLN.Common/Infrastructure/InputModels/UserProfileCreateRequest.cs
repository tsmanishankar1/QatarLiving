using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.InputModels
{
    public class UserProfileCreateRequest
    {
        [Required]
        public string Firstname { get; set; } = null!;
        [Required]
        public string Lastname { get; set; } = null!;
        [Required]
        public DateOnly Dateofbirth { get; set; }
        [Required]
        public string Gender { get; set; } = null!;
        [Required]
        public string Mobilenumber { get; set; } = null!;
        [Required]
        public string Emailaddress { get; set; } = null!;
        [Required]
        public string Nationality { get; set; } = null!;
        [Required]
        public string Password { get; set; } = null!;
        [Required]
        public string Confirmpassword { get; set; } = null!;
        public string? Languagepreferences { get; set; }
        public string? Location { get; set; }
    }
    public class EmailRequest
    {
        [Required]
        public string Email { get; set; } = null!;
    }
    public class OtpVerificationRequest
    {
        [Required]
        public string Otp { get; set; } = null!;
    }

}
