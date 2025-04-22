

namespace QLN.Common.DTO_s
{
    public class LoginResponse
    {
        public string Username { get; set; } = null!;
        public string Mobilenumber { get; set; } = null!;
        public string Emailaddress { get; set; } = null!;
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;     
        public bool? IsTwoFactorEnabled { get; set; }
    }

    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class RefreshTokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }
}
