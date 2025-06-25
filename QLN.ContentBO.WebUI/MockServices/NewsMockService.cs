using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Services.Base;
using System.Net;
using System.Text;
using System.Text.Json;

namespace QLN.ContentBO.WebUI.MockServices
{
    public class NewsMockService : ServiceBase<NewsMockService>, INewsService
    {
        public NewsMockService(HttpClient httpClientDI, ILogger<NewsMockService> Logger)
           : base(httpClientDI, Logger)
        {

        }

        private List<NewsArticleDTO> newsArticles =
        [
        new()
        {
            Id = Guid.NewGuid(),
            Title = "Qatar Hosts Global Climate Summit",
            Content = "<p>Qatar welcomed leaders from around the world to discuss urgent climate goals...</p>",
            WriterTag = "Amal.Hassan",
            CoverImageUrl = "https://cdn.example.com/images/climate-summit.jpg",
            InlineImageUrls = new()
            {
                "https://cdn.example.com/images/venue.jpg",
                "https://cdn.example.com/images/panel.jpg"
            },
            Categories = new()
            {
                new() { CategoryId = 1, SubcategoryId = 101, SlotId = 1 }, // News > Qatar
                new() { CategoryId = 1, SubcategoryId = 102, SlotId = 2 }  // News > Middle East
            },
            PublishedDate = DateTime.UtcNow.AddDays(-1),
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            UpdatedBy = Guid.NewGuid(),
            UpdatedAt = DateTime.UtcNow
        },

        new()
        {
            Id = Guid.NewGuid(),
            Title = "Startup Investment Rises in Asia",
            Content = "<p>Asian startups raised record-breaking funding this quarter...</p>",
            WriterTag = "Ravi.Patel",
            CoverImageUrl = "https://cdn.example.com/images/startup.jpg",
            InlineImageUrls = new()
            {
                "https://cdn.example.com/images/chart-growth.png"
            },
            Categories = new()
            {
                new() { CategoryId = 2, SubcategoryId = 201, SlotId = 101 } // Business > Startups
            },
            PublishedDate = DateTime.UtcNow.AddDays(-3),
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow.AddDays(-4),
            UpdatedBy = Guid.NewGuid(),
            UpdatedAt = DateTime.UtcNow.AddDays(-2)
        },

        new()
        {
            Id = Guid.NewGuid(),
            Title = "Football World Cup 2030: Host Countries Announced",
            Content = "<p>FIFA has officially confirmed the hosts for the 2030 World Cup...</p>",
            WriterTag = "Sports.Desk",
            CoverImageUrl = "https://cdn.example.com/images/world-cup.jpg",
            InlineImageUrls = new()
            {
                "https://cdn.example.com/images/logo2030.png"
            },
            Categories = new()
            {
                new() { CategoryId = 3, SubcategoryId = 301, SlotId = 2 } // Sports > Football
            },
            PublishedDate = DateTime.UtcNow.AddDays(-7),
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedBy = Guid.NewGuid(),
            UpdatedAt = DateTime.UtcNow.AddDays(-8)
        },

        new()
        {
            Id = Guid.NewGuid(),
            Title = "AI Trends in 2025: What's Next?",
            Content = "<p>Experts predict major breakthroughs in generative AI and robotics...</p>",
            WriterTag = "Tech.Journal",
            CoverImageUrl = "https://cdn.example.com/images/ai-future.jpg",
            InlineImageUrls = new()
            {
                "https://cdn.example.com/images/robot.jpg",
                "https://cdn.example.com/images/ai-chart.png"
            },
            Categories = new()
            {
                new() { CategoryId = 2, SubcategoryId = 202, SlotId = 1 }, // Business > Technology
                new() { CategoryId = 4, SubcategoryId = 401, SlotId = 1 }  // Lifestyle > Innovation
            },
            PublishedDate = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedBy = Guid.NewGuid(),
            UpdatedAt = DateTime.UtcNow
        },

        new()
        {
            Id = Guid.NewGuid(),
            Title = "Wellness Retreats Booming in 2025",
            Content = "<p>The wellness tourism industry is experiencing a significant rise post-pandemic...</p>",
            WriterTag = "Maya.Wellness",
            CoverImageUrl = "https://cdn.example.com/images/spa.jpg",
            InlineImageUrls = new()
            {
                "https://cdn.example.com/images/nature.png"
            },
            Categories = new()
            {
                new() { CategoryId = 4, SubcategoryId = 402, SlotId = 102 } // Lifestyle > Wellness
            },
            PublishedDate = DateTime.UtcNow.AddDays(-5),
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow.AddDays(-6),
            UpdatedBy = Guid.NewGuid(),
            UpdatedAt = DateTime.UtcNow.AddDays(-5)
        }
    ];

        private List<NewsCategory> newsCateg =
            [
                    new()
                    {
                        Id = 1,
                        CategoryName = "News",
                        SubCategories = new List<NewsSubCategory>() {
                            new() { Id = 1, CategoryName = "Qatar" },
                            new() { Id = 2, CategoryName = "Middle East" },
                        }
                    },

                    new()
                    {
                        Id = 2,
                        CategoryName = "Business",
                        SubCategories = [
                            new() { Id = 1, CategoryName = "QatarEconomy" },
                            new() { Id = 2, CategoryName = "MarketUpdates" }
                        ]
                    },

                    new()
                    {
                        Id = 3,
                        CategoryName = "Sports",
                        SubCategories = [
                            new() { Id = 1, CategoryName = "Qatar Sports" },
                            new() { Id = 2, CategoryName = "FootBall" }
                        ]
                    },

                    new()
                    {
                        Id = 4,
                        CategoryName = "LifeStyle",
                        SubCategories = [
                            new() { Id = 1, CategoryName = "Food & Dining" },
                            new() { Id = 2, CategoryName = "Travel & Leisure" }
                        ]
                    }
            ];

        private List<string> writerTags =
            [
                    "Qatar Living",
                    "Everything Qatar",
                    "FIFA Arab Cup",
                    "QL Exclusive",
                    "Advice & Help"
            ];

        private List<Slot> slots =
            [
                new() {Id = 1, Name = "Slot1"},
                new() {Id = 2, Name = "Slot2"},
                new() {Id = 3, Name = "Slot3"},
                new() {Id = 4, Name = "Slot4"},
                new() {Id = 5, Name = "Slot5"},
                new() {Id = 6, Name = "Slot6"},
                new() {Id = 7, Name = "Slot7"},
                new() {Id = 8, Name = "Slot8"},
                new() {Id = 9, Name = "Slot9"},
                new() {Id = 10, Name = "Slot10"},
                new() {Id = 11, Name = "Slot11"},
                new() {Id = 12, Name = "Slot12"},
                new() {Id = 13, Name = "Slot12"},
                new() {Id = 14, Name = "Published"},
                new() {Id = 15, Name = "UnPublished"}
            ];

        public Task<HttpResponseMessage> CreateArticle(NewsArticleDTO newsArticle)
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseMessage> GetAllArticles()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(newsArticles), Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
        }

        public Task<HttpResponseMessage> GetNewsCategories()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(newsCateg), Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
        }

        public Task<HttpResponseMessage> GetSlots()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(slots), Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
        }

        public Task<HttpResponseMessage> GetWriterTags()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(writerTags), Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
        }

        public Task<HttpResponseMessage> UpdateArticle(NewsArticleDTO newsArticle)
        {
            throw new NotImplementedException();
        }
    }
}
