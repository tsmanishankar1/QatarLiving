namespace QLN.Common.DTO_s
{
    public class AnalyticsEventRequest
    {
        public string Section { get; set; } = default!; 
        public string EntityId { get; set; } = default!;
        public long Impressions { get; set; }
        public long Views { get; set; }
        public long WhatsApp { get; set; }
        public long Calls { get; set; }
        public long Shares { get; set; }
        public long Saves { get; set; }
    }
}
