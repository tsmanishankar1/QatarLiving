namespace QLN.ContentBO.WebUI.Models
{
     public class ListingItemsTransaction

        {
            public string AdId { get; set; } // string to allow "N/A"
    public string OrderId { get; set; }
    public string ProductType { get; set; }
    public string UserName { get; set; }
    public string Status { get; set; }
    public string Email { get; set; }
    public string Mobile { get; set; }
    public string WhatsApp { get; set; }
    public string Amount { get; set; }
    public DateTime? CreationDate { get; set; }
    public DateTime? PublishedDate { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
        }
}
