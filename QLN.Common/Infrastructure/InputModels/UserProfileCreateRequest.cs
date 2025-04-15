using System.ComponentModel.DataAnnotations;

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
       
        public string? Languagepreferences { get; set; }
        public string? Location { get; set; }
    }
    public class AccountVerification
    {
        [Required]
        public string Email { get; set; } = null!;
        [Required]
        public string Otp { get; set; } = null!;
    }
    public class EmailRequest
    {
        [Required]
        public string Name { get; set; } = null!;
    }
    public class OtpVerificationRequest
    {
        [Required]
        public string Name { get; set; } = null!;
        public string? PasswordOrOtp { get; set; }
      
    }
    public class LoginResponse
    {
        public string JwtToken { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
    }
}
