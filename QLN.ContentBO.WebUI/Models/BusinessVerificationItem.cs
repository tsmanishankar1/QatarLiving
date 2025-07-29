namespace QLN.ContentBO.WebUI.Models
{
    public class BusinessVerificationItem
    {
        public int UserId { get; set; } 
        public string BusinessName { get; set; }
        public string UserName { get; set; }
        public string CRFile { get; set; }
        public string CRLicense { get; set; }
        public DateTime EndDate { get; set; }
    }
}