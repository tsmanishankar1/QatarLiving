using System;
using System.Collections.Generic;

namespace QLN.Common.DTO_s
{
    public class ContentNewsDto
    {
        // Shared class for item content
        public class Item
        {
            public Guid id { get; set; }
            public string page_name { get; set; }
            public string queue_name { get; set; }
            public string queue_label { get; set; }
            public string node_type { get; set; }
            public string date_created { get; set; }
            public string image_url { get; set; }
            public string user_name { get; set; }
            public string title { get; set; }
            public string slug { get; set; }
            public Category category { get; set; }
            public string category_id { get; set; }
            public string description { get; set; }
        }

        public enum Category
        {
            None = 0,         // default fallback
            News=1,
            Business=2,
            Sports=3,
            Lifestyle=4,
        }
        // Enum for possible queue labels
        public enum QueueLabel
        {
            Articles1 = 1,
            Articles2 = 2,
            MoreArticles = 3,
            MostPopularArticles = 4,
            TopStory = 5,
            WatchonQatarLiving = 6,
        }

        // Represents a section of articles under a queue label
        public class ArticleSection
        {
            public QueueLabel queue_label { get; set; }
            public Item[] items { get; set; }
        }

        // Finance Topics
        public enum FinanceTopic
        {
            Entrepreneurship,
            Finance,
            JobsCareers,
            MarketUpdate,
            Qatar,
            RealEstate
        }

        // Lifestyle Topics
        public enum LifestyleTopic
        {
            ArtsCulture,
            Events,
            FashionStyle,
            FoodDining,
            HomeLiving,
            TravelLeisure
        }

        // News Topics
        public enum NewsTopic
        {
            Community,
            HealthEducation,
            Law,
            MiddleEast,
            Qatar,
            World
        }

        // Sports Topics
        public enum SportsTopic
        {
            AthleteFeatures,
            Football,
            International,
            Motorsports,
            Olympics,
            QatarSports
        }

        // Dictionary to hold Finance articles by topic
        public class Finance
        {
            public Dictionary<FinanceTopic, ArticleSection> Topics { get; set; } = new();
        }

        // Dictionary to hold Lifestyle articles by topic
        public class Lifestyle
        {
            public Dictionary<LifestyleTopic, ArticleSection> Topics { get; set; } = new();
        }

        // Dictionary to hold News articles by topic
        public class News
        {
            public Dictionary<NewsTopic, ArticleSection> Topics { get; set; } = new();
        }

        // Dictionary to hold Sports articles by topic
        public class Sports
        {
            public Dictionary<SportsTopic, ArticleSection> Topics { get; set; } = new();
        }
    }
}
