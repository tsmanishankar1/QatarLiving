namespace QLN.Notification.MS.Dto
{
    public class SmtpSettings
    {
        public string Host { get; set; }
        public string Port { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string DisplayName { get; set; }
        public bool SSL { get; set; }
        public bool DefaultCredential { get; set; }
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
