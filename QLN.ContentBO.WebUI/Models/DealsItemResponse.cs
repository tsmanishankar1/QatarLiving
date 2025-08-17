namespace QLN.ContentBO.WebUI.Models
{
    public class DealsItemResponse
    {
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public List<DealsItem> Items { get; set; } = [];
    }

    public class DealsItem
    {
        public long AdId { get; set; }
        public string Dealtitle { get; set; }
        public string subscriptiontype { get; set; }
        public DateTime DateCreated { get; set; }
        public string Weburl { get; set; }
        public int WebClick { get; set; }
        public int Views { get; set; }
        public int Impression { get; set; }
        public int Phonelead { get; set; }
        public string email { get; set; }
        public string createdby { get; set; }
        public string ContactNumber { get; set; }
        public string WhatsappNumber { get; set; }
        public string CoverImage { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsPromoted { get; set; }
    }
}
