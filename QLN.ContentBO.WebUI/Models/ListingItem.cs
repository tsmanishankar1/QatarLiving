namespace QLN.ContentBO.WebUI.Models
{
     public class ListingItem
        {
            public int AdId { get; set; }
            public int UserId { get; set; }
            public string AdTitle { get; set; }
            public int InternalUserId { get; set; }
            public string UserName { get; set; }
            public string Category { get; set; }
            public string SubCategory { get; set; }
            public string Section { get; set; }
            public DateTime CreationDate { get; set; }
            public DateTime PublishedDate { get; set; }
            public DateTime ExpiryDate { get; set; }
            public string ImageUrl { get; set; }
        }
}
