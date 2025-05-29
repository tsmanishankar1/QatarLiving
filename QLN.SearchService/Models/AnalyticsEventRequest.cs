namespace QLN.SearchService.Models
{
    public class AnalyticsEventRequest
    {
        public string Section { get; set; } = default!; // e.g. "ad", "banner", "subscribeNow"
        public string EntityId { get; set; } = default!; // e.g. AdId, BannerId, etc.
        public long Impressions { get; set; }
        public long Views { get; set; }
        public long WhatsApp { get; set; }
        public long Calls { get; set; }
        public long Shares { get; set; }
        public long Saves { get; set; }
    }
}
