using Microsoft.AspNetCore.Components;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Components.ViewToggleButtons;
using QLN.Web.Shared.Model;
public class NewsBase : ComponentBase
{
    protected bool IsDisliked { get; set; } = true;
    protected List<string> carouselImages = new()
    {
        "/images/banner_image.svg",
        "/images/banner_image.svg",
        "/images/banner_image.svg"
    };
    protected List<ViewToggleButtons.ViewToggleOption> _viewOptions = new()
    {
        new() { Label = "News", Value = "news" },
        new() { Label = "Finance", Value = "finance" },
        new() { Label = "Sports", Value = "sports" },
        new() { Label = "Lifestyle", Value = "lifestyle" }
    };
    
     
    protected string _selectedView = "news";
    protected async void SetViewMode(string view)
    {
         _selectedView = view;
    }
    protected NewsItem GoldNews = new NewsItem
    {
        Category = "Finance",
        Title = "Qatar gold prices rise by 4.86% this week",
        ImageUrl = "/images/gold.svg"
    };
    protected List<NewsItem> GoldNewsList = new()
    {
    new NewsItem
    {
        Category = "Finance",
        Title = "Qatar gold prices rise by 4.86% this week",
        ImageUrl = "/images/gold.svg"
    },
    new NewsItem
    {
         Category = "Finance",
        Title = "Hilton Salwa Beach Resort & Villas unveils Summer",
        ImageUrl = "/images/content/qatar_Image2.svg"
    },
    new NewsItem
    {
        Category = "Finance",
        Title = "Qatar’s Abdulwahab creates history, enters Round of 64",
        ImageUrl = "/images/content/qatar_image.svg"
    },
    new NewsItem
    {
        Category = "Finance",
        Title = "Qatar gold prices rise by 4.86% this week",
        ImageUrl = "/images/gold.svg"
    },
    new NewsItem
    {
         Category = "Finance",
        Title = "Hilton Salwa Beach Resort & Villas unveils Summer",
        ImageUrl = "/images/content/qatar_Image2.svg"
    },
    new NewsItem
    {
        Category = "Finance",
        Title = "Qatar’s Abdulwahab creates history, enters Round of 64",
        ImageUrl = "/images/content/qatar_image.svg"
    },
    new NewsItem
    {
        Category = "Finance",
        Title = "Qatar gold prices rise by 4.86% this week",
        ImageUrl = "/images/gold.svg"
    },
    new NewsItem
    {
         Category = "Finance",
        Title = "Hilton Salwa Beach Resort & Villas unveils Summer",
        ImageUrl = "/images/content/qatar_Image2.svg"
    },
    new NewsItem
    {
        Category = "Finance",
        Title = "Qatar’s Abdulwahab creates history, enters Round of 64",
        ImageUrl = "/images/content/qatar_image.svg"
    }
};
    protected string[] Tabs = new[] { "Qatar", "Sports", "Finance", "Lifestyle", "Politics", "Option" };
    protected string SelectedTab = "Qatar";
    protected void SelectTab(string tab)
    {
        SelectedTab = tab;
    }
}