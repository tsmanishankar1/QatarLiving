namespace QLN.ContentBO.WebUI.Models
{
    public class UserSyncResponse
    {
        public string Username { get; set; }
        public string MobileNumber { get; set; }
        public string EmailAddress { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public bool IsTwoFactorEnabled { get; set; }
    }
}
