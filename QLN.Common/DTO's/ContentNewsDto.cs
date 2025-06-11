using System;
using System.Collections.Generic;

namespace QLN.Common.DTO_s
{
    public class ContentNewsDto
    {
        public Finance FinanceData { get; set; }
        public Lifestyle LifestyleData { get; set; }
        public News NewsData { get; set; }
        public Sports SportsData { get; set; }
    }

    // Enum for major categories
    public enum V2Category
    {
        None = 0,
        News = 1,
        Business = 2,
        Sports = 3,
        Lifestyle = 4
    }

    // Enum for queue labels
    public enum QueueLabel
    {
        Articles1 = 1,
        Articles2 = 2,
        MoreArticles = 3,
        MostPopularArticles = 4,
        TopStory = 5,
        WatchonQatarLiving = 6
    }

    // Shared item structure
    public class Item
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string PageName { get; set; }
        public string QueueName { get; set; }
        public string QueueLabel { get; set; }
        public string NodeType { get; set; }
        public string DateCreated { get; set; }
        public string ImageUrl { get; set; }
        public string UserName { get; set; }
        public string Title { get; set; }
        public string Slug { get; set; }
        public V2Category Category { get; set; }
        public string CategoryId { get; set; }
        public string Description { get; set; }
    }

    // Generic section for articles
    public class ArticleSection
    {
        public QueueLabel QueueLabel { get; set; }
        public List<Item> Items { get; set; }
    }

    // Enums for subcategories
    public enum FinanceTopic
    {
        Entrepreneurship,
        Finance,
        JobsCareers,
        MarketUpdate,
        Qatar,
        RealEstate
    }

    public enum LifestyleTopic
    {
        ArtsCulture,
        Events,
        FashionStyle,
        FoodDining,
        HomeLiving,
        TravelLeisure
    }

    public enum NewsTopic
    {
        Community,
        HealthEducation,
        Law,
        MiddleEast,
        Qatar,
        World
    }

    public enum SportsTopic
    {
        AthleteFeatures,
        Football,
        International,
        Motorsports,
        Olympics,
        QatarSports
    }

    // Main content categories
    public class Finance
    {
        public Dictionary<FinanceTopic, ArticleSection> Topics { get; set; } = new();
    }

    public class Lifestyle
    {
        public Dictionary<LifestyleTopic, ArticleSection> Topics { get; set; } = new();
    }

    public class News
    {
        public Dictionary<NewsTopic, ArticleSection> Topics { get; set; } = new();
    }

    public class Sports
    {
        public Dictionary<SportsTopic, ArticleSection> Topics { get; set; } = new();
    }

    // Optional: You can define a response DTO if needed
    public class NewsSummary
    {
        public int TotalSections { get; set; }
        public Dictionary<string, int> TopicBreakdown { get; set; }
        public DateTime ProcessedAt { get; set; }
    }
    public class ContentNewsRequestByIdDto
    {
        public string UserId { get; set; }
        public ContentNewsDto NewsContent { get; set; }
    }

}
