namespace QLN.ContentBO.WebUI.Models
{
    public class UserModal
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string CRFileName { get; set; }
        public string CRFileURL { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime PublishedDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string ImageUrl { get; set; }
    }
}
