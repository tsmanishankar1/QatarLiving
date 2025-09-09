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

        public string? Link { get; set; }

    }

    public class BannerResponse

    {

        // Data in the response that I have attempted to connect to what it appears to be for

        [JsonPropertyName("qln_banners_daily_hero")]

        public List<BannerItem> QlnBannersDailyHero { get; set; } = new();

        [JsonPropertyName("qln_banners_news_world_take_over_1")]

        public List<BannerItem> QlnBannersNewsWorldTakeOver1 { get; set; } = new();

        [JsonPropertyName("qln_banners_news_middle_east_hero")]

        public List<BannerItem> QlnBannersNewsMiddleEastHero { get; set; } = new();

        [JsonPropertyName("qln_banners_daily_take_over_1")]

        public List<BannerItem> QlnBannersDailyTakeOver1 { get; set; } = new();

        [JsonPropertyName("qln_banners_news_qatar_take_over_1")]

        public List<BannerItem> QlnBannersNewsQatarTakeOver1 { get; set; } = new();

        [JsonPropertyName("qln_banners_daily_take_over_2")]

        public List<BannerItem> QlnBannersNewsQatarTakeOver2 { get; set; } = new();

        // Original list of mappings, some are null on the current response from existing QL API and so we don't have this to display

        [JsonPropertyName("content_daily_hero")]

        public List<BannerItem> ContentDailyHero { get; set; } = new();

        [JsonPropertyName("content_daily_takeover_first")]

        public List<BannerItem> ContentDailyTakeoverFirst { get; set; } = new();

        [JsonPropertyName("content_daily_takeover_second")]

        public List<BannerItem> ContentDailyTakeoverSecond { get; set; } = new();

        [JsonPropertyName("content_news_hero")]

        public List<BannerItem> ContentNewsHero { get; set; } = new();

        [JsonPropertyName("content_news_takeover")]

        public List<BannerItem> ContentNewsTakeover { get; set; } = new();

        [JsonPropertyName("content_news_side")]

        public List<BannerItem> ContentNewsSide { get; set; } = new();

        [JsonPropertyName("content_article_hero")]

        public List<BannerItem> ContentArticleHero { get; set; } = new();

        [JsonPropertyName("content_article_side")]

        public List<BannerItem> ContentArticleSide { get; set; } = new();

        [JsonPropertyName("content_events_hero")]

        public List<BannerItem> ContentEventsHero { get; set; } = new();

        [JsonPropertyName("content_events_detail_hero")]

        public List<BannerItem> ContentEventsDetailHero { get; set; } = new();

        [JsonPropertyName("content_events_detail_side")]

        public List<BannerItem> ContentEventsDetailSide { get; set; } = new();

        [JsonPropertyName("content_community_hero")]

        public List<BannerItem> ContentCommunityHero { get; set; } = new();

        [JsonPropertyName("content_community_side")]

        public List<BannerItem> ContentCommunitySide { get; set; } = new();

        [JsonPropertyName("content_community_post_hero")]

        public List<BannerItem> ContentCommunityPostHero { get; set; } = new();

        [JsonPropertyName("content_community_post_side")]

        public List<BannerItem> ContentCommunityPostSide { get; set; } = new();

        [JsonPropertyName("content_videos_hero")]

        public List<BannerItem> ContentVideosHero { get; set; } = new();

        [JsonPropertyName("content_videos_takeover")]

        public List<BannerItem> ContentVideosTakeover { get; set; } = new();

        [JsonPropertyName("content_topics_hero")]

        public List<BannerItem> ContentTopicsHero { get; set; } = new();

        [JsonPropertyName("content_topics_takeover")]

        public List<BannerItem> ContentTopicsTakeover { get; set; } = new();

    }

}

