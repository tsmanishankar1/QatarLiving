namespace QLN.Notification.MS.Dto
{
    public class SmtpSettings
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string DisplayName { get; set; }
        public bool UseSsL { get; set; }
        public bool UseDefaultCredentials { get; set; }
    }
    public class SmsSettings
    {
        public string CustomerId { get; set; }
        public string UserName { get; set; }
        public string UserPassword { get; set; }
        public string Originator { get; set; }
        public string ApiUrl { get; set; }
    }
}
