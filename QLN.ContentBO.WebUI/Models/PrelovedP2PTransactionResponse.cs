namespace QLN.ContentBO.WebUI.Models
{
    public class PrelovedP2PTransactionResponse
    {
        public List<PrelovedP2PTransactionItem> Records { get; set; } = [];
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}

