using Microsoft.AspNetCore.Components;
public class MoreArticleSectionBase : ComponentBase
{
    public class Article
    {
        public string Category { get; set; }
        public string Title { get; set; }
        public string ImageUrl { get; set; }
        public string Url { get; set; }
    }

    protected List<Article> Articles = new()
    {
        new Article
        {
            Category = "Lifestyle",
            Title = "How to spot scam websites & social media accounts in Qatar",
            Url = "#",
            ImageUrl = "/images/sample_article.svg"
        },
        new Article
        {
            Category = "Finance",
            Title = "Qatar gold prices rise by 4.86% this week",
            Url = "#",
            ImageUrl = "/images/sample_article.svg"
        },
        new Article
        {
            Category = "International",
            Title = "Saudi Arabia announces Umrah season calendar",
            Url = "#",
            ImageUrl = "/images/sample_article.svg"
        },
        new Article
        {
            Category = "Entertainment",
            Title = "Qatar Museums to feature new publications at 34th Doha International Book Fair",
            Url = "#",
            ImageUrl = "/images/sample_article.svg"
        },
        new Article
        {
            Category = "Sports",
            Title = "Qatar to play Bahrain in the final of the Arab Handball Cup on Sunday",
            Url = "#",
            ImageUrl = "/images/sample_article.svg"
        },
        new Article
        {
            Category = "Sports",
            Title = "Qatar to play Bahrain in the final of the Arab Handball Cup on Sunday",
            Url = "#",
            ImageUrl = "/images/sample_article.svg"
        }
    };
}