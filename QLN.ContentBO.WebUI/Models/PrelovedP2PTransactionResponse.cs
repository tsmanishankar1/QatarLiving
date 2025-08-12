namespace QLN.ContentBO.WebUI.Models
{
    public class PrelovedP2PTransactionResponse
    {
        public List<PrelovedP2PTransactionItem> Items { get; set; } = [];
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PerPage { get; set; }
    }
}

