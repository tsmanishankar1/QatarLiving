namespace QLN.ContentBO.WebUI.Models
{
    public class TokenV2Response
    {
        public string UserName { get; set; }
        public string Mobilenumber { get; set; }
        public string Emailaddress { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public bool isTwoFactorEnabled { get; set; }
    }
}
