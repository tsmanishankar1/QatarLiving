namespace QLN.ContentBO.WebUI.Models
{
    public class CompanyUpdateActions
    {
        public Guid? CompanyId { get; set; }
        public VerifiedStatus? Status { get; set; }
        public VerifiedStatus? CompanyVerificationStatus { get; set; }
    }
}