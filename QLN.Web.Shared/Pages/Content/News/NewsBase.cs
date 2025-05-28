using QLN.Web.Shared.Components.ViewToggleButtons;
using QLN.Web.Shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Web.Shared.Services.Interface;
using System.Net.Http.Json;

namespace QLN.Web.Shared.Pages.Content.News
{
    public class NewsCardBase : ComponentBase
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
        ImageUrl = "/images/gold.svg"
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
        ImageUrl = "/images/gold.svg"
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
        ImageUrl = "/images/gold.svg"
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
        [Inject]
        protected NavigationManager navManager { get; set; }
        [Parameter]
        public NewsItem Item { get; set; }
        [Parameter]
        public bool IsHorizontal { get; set; } = false;
        protected void NavigateToDetails()
        {
            navManager.NavigateTo("/article/details");
        }
        [Inject] private ILogger<NewsCardBase> Logger { get; set; }
        [Inject] private INewsService _newsService { get; set; }
        protected NewsQatarPageResponse QatarNewsContent { get; set; } = new NewsQatarPageResponse();
        protected NewsCommunityPageResponse CommunityNewsContent { get; set; } = new NewsCommunityPageResponse();
        protected NewsHealthEducationPageResponse HealthEducationNewsContent { get; set; } = new NewsHealthEducationPageResponse();
        protected NewsLawPageResponse LawsNewsContent { get; set; } = new NewsLawPageResponse();
        protected NewsMiddleEastPageResponse MiddleEastNewsContent { get; set; } = new NewsMiddleEastPageResponse();
        protected NewsWorldPageResponse WorldNewsContent { get; set; } = new NewsWorldPageResponse();
        protected async override Task OnInitializedAsync()
        {
            try
            {
                QatarNewsContent = await GetNewsQatarAsync();
                CommunityNewsContent = await GetNewsCommunityAsync();
                HealthEducationNewsContent = await GetNewsHealthAndEducationAsync();
                LawsNewsContent = await GetNewsLawAsync();
                MiddleEastNewsContent = await GetNewsMiddleEastAsync();
                WorldNewsContent = await GetNewsWorldAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "OnInitializedAsync");
            }
        }

        /// <summary>
        /// Gets Content Landing Page data
        /// </summary>
        /// <returns>NewsQatarPageResponse</returns>
        protected async Task<NewsQatarPageResponse> GetNewsQatarAsync()
        {
            try
            {
                var apiResponse = await _newsService.GetNewsQatarAsync() ?? new HttpResponseMessage();
                if (apiResponse.IsSuccessStatusCode && apiResponse.Content != null)
                {
                    var response = await apiResponse.Content.ReadFromJsonAsync<NewsQatarPageResponse>();
                    return response ?? new NewsQatarPageResponse();
                }
                return new NewsQatarPageResponse();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetNewsQatarAsync");
                return new NewsQatarPageResponse();
            }
        }
        protected async Task<NewsCommunityPageResponse> GetNewsCommunityAsync()
        {
            try
            {
                var apiResponse = await _newsService.GetNewsCommunityAsync() ?? new HttpResponseMessage();

                if (apiResponse.IsSuccessStatusCode && apiResponse.Content != null)
                {
                    var response = await apiResponse.Content.ReadFromJsonAsync<NewsCommunityPageResponse>();
                    return response ?? new NewsCommunityPageResponse();
                }
                return new NewsCommunityPageResponse();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetNewsCommunityAsync");
                return new NewsCommunityPageResponse();
            }
        }
        protected async Task<NewsHealthEducationPageResponse> GetNewsHealthAndEducationAsync()
        {
            try
            {
                var apiResponse = await _newsService.GetNewsHealthAndEducationAsync() ?? new HttpResponseMessage();

                if (apiResponse.IsSuccessStatusCode && apiResponse.Content != null)
                {
                    var response = await apiResponse.Content.ReadFromJsonAsync<NewsHealthEducationPageResponse>();
                    return response ?? new NewsHealthEducationPageResponse();
                }
                return new NewsHealthEducationPageResponse();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetNewsHealthAndEducationAsync");
                return new NewsHealthEducationPageResponse();
            }
        }
        protected async Task<NewsLawPageResponse> GetNewsLawAsync()
        {
            try
            {
                var apiResponse = await _newsService.GetNewsLawAsync() ?? new HttpResponseMessage();

                if (apiResponse.IsSuccessStatusCode && apiResponse.Content != null)
                {
                    var response = await apiResponse.Content.ReadFromJsonAsync<NewsLawPageResponse>();
                    return response ?? new NewsLawPageResponse();
                }
                return new NewsLawPageResponse();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GGetNewsLawAsync");
                return new NewsLawPageResponse();
            }
        }
        protected async Task<NewsMiddleEastPageResponse> GetNewsMiddleEastAsync()
        {
            try
            {
                var apiResponse = await _newsService.GetNewsMiddleEastAsync() ?? new HttpResponseMessage();

                if (apiResponse.IsSuccessStatusCode && apiResponse.Content != null)
                {
                    var response = await apiResponse.Content.ReadFromJsonAsync<NewsMiddleEastPageResponse>();
                    return response ?? new NewsMiddleEastPageResponse();
                }
                return new NewsMiddleEastPageResponse();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetNewsMiddleEastAsync");
                return new NewsMiddleEastPageResponse();
            }
        }
        protected async Task<NewsWorldPageResponse> GetNewsWorldAsync()
        {
            try
            {
                var apiResponse = await _newsService.GetNewsWorldAsync() ?? new HttpResponseMessage();

                if (apiResponse.IsSuccessStatusCode && apiResponse.Content != null)
                {
                    var response = await apiResponse.Content.ReadFromJsonAsync<NewsWorldPageResponse>();
                    return response ?? new NewsWorldPageResponse();
                }
                return new NewsWorldPageResponse();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetNewsWorldAsync");
                return new NewsWorldPageResponse();
            }
        }
    }
}
