using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace QLN.Common.Infrastructure.DTO_s
{
    public class BannerItem
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("alt")]
        public string Alt { get; set; }

        [JsonPropertyName("duration")]
        public string Duration { get; set; }

        [JsonPropertyName("image_desktop")]
        public string ImageDesktop { get; set; }

        [JsonPropertyName("image_mobile")]
        public string ImageMobile { get; set; }

        [JsonPropertyName("link")]
        public string Link { get; set; }
    }

    public class BannerResponse
    {
        [JsonPropertyName("qln_banners_daily_hero")]
        public List<BannerItem> DailyHero { get; set; } = new();

        [JsonPropertyName("qln_banners_daily_take_over_1")]
        public List<BannerItem> DailyTakeOver1 { get; set; } = new();

        [JsonPropertyName("qln_banners_daily_take_over_2")]
        public List<BannerItem> DailyTakeOver2 { get; set; } = new();

        [JsonPropertyName("qln_banners_news_qatar_hero")]
        public List<BannerItem> NewsQatarHero { get; set; } = new();

        [JsonPropertyName("qln_banners_news_qatar_take_over_1")]
        public List<BannerItem> NewsQatarTakeOver1 { get; set; } = new();
    }
}
