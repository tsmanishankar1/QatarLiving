

using System.ComponentModel.DataAnnotations;

namespace QLN.Common.DTO_s
{
    public class RegisterRequest
    {
        [Required]
        public string Username { get; set; } = null!;
        [Required]
        public string FirstName { get; set; } = null!;
        [Required]
        public string Lastname { get; set; } = null!;
        [Required]
        public DateOnly Dateofbirth { get; set; }
        
        public string MobileOperator { get; set; } = null;
        [Required]
        public string Mobilenumber { get; set; } = null!;
        [Required]
        public string Emailaddress { get; set; } = null!;
        [Required]
        public string Nationality { get; set; } = null!;
        [Required]
        public string Password { get; set; } = null!;
        public string? Languagepreferences { get; set; }
        public bool TwoFactorEnabled { get; set; } = false;
    }

    public class UpdateProfileRequest
    {
        [Required]
        public string FirstName { get; set; } = null!;
        [Required]
        public string LastName { get; set; } = null!;
        [Required]
        public DateOnly Dateofbirth { get; set; }
        [Required]
        public string Gender { get; set; } = null!;
        [Required]
        public string MobileNumber { get; set; } = null!;
        [Required]
        public string Nationality { get; set; } = null!;        
        public string? Languagepreferences { get; set; }
    }

}
