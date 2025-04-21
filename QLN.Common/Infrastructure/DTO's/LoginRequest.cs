
namespace QLN.Common.DTO_s
{
    public class LoginRequest
    {
        public string UsernameOrEmailOrPhone { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class TwoFactorToggleRequest
    {
        public string EmailorPhoneNumber { get; set; }
        public bool Enable { get; set; }
    }

    public class Verify2FARequest
    {
        public string UsernameOrEmailOrPhone { get; set; } = null!;
        public string TwoFactorCode { get; set; } = null!;
    }

}
