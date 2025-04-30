
using System.ComponentModel.DataAnnotations;

namespace QLN.Common.DTO_s
{
    public class LoginRequest
    {
        [Required]
        public string UsernameOrEmailOrPhone { get; set; } = string.Empty;
        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class TwoFactorToggleRequest
    {
        [Required]
        public string EmailorPhoneNumber { get; set; }
        [Required]
        public bool Enable { get; set; }
    }

    public class Verify2FARequest
    {
        [Required]
        public string UsernameOrEmailOrPhone { get; set; } = null!;
        [Required]
        public string TwoFactorCode { get; set; } = null!;
    }

}
