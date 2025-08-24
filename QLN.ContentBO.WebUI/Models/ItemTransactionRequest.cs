namespace QLN.ContentBO.WebUI.Models
{
    public class ItemTransactionRequest
    {
        public int SubVertical { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string? SearchText { get; set; }
        public string? ProductType { get; set; }
        public string? DateCreated { get; set; }
        public string? DatePublished { get; set; }
        public string? DateStart { get; set; }
        public string? DateEnd { get; set; }
        public string? Status { get; set; }
        public string? PaymentMethod { get; set; }
        public string? SortBy { get; set; }
        public string? SortOrder { get; set; }
    }
}
