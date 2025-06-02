using QLN.Web.Shared.Services;

namespace QLN.Web.Shared.Models
{
        public class QLAnalyticsCallProps : ActionTrackingProps
        {
            public int AnalyticType { get; set; }
            public int VerticalTag { get; set; }
        public int[]? AdId { get; set; }
            public object? Filters { get; set; }
            public Dictionary<string, string>? AdditionalTag { get; set; }
            public string[]? Banner { get; set; }
            public string? Token { get; set; }
            public string? Number { get; set; }
            public string? Lead { get; set; }
            public string? Url { get; set; }
            public int[]? Impressions { get; set; }
            public int? View { get; set; }
        }
}
