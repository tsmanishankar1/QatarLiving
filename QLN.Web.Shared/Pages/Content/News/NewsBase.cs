using QLN.Web.Shared.Components.ViewToggleButtons;
using QLN.Web.Shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Web.Shared.Services.Interface;
using System.Net.Http.Json;

namespace QLN.Web.Shared.Pages.Content.News
{
    public class NewsBase : ComponentBase
    {
        public bool isLoading = true;
        public List<Category> Categories { get; set; } = new List<Category>
    {
        new Category
        {
            Name = "News",
            SubCategories = new List<string> { "Qatar", "Middle East", "World","Health & Education","Community","Law" }
        },
        new Category
        {
            Name = "Business",
            SubCategories = new List<string> { "Qatar Economy", "Market Updates", "Real Estate","Entrepreneurship","Finance","Jobs & Careers" }
        },
        new Category
        {
            Name = "Sports",
            SubCategories = new List<string> { "Qatar Sports", "Football", "International","Motorsports","Olympics","Athlete Features" }
        },
        new Category
        {
            Name = "Lifestyle",
            SubCategories = new List<string> { "Food & Dining", "Travel & Leisure", "Arts & Culture","Events","Fashion & Style","Home & Living" }
        }
    };
        protected List<string> carouselImages = new()
    {
        "/images/banner_image.svg",
        "/images/banner_image.svg",
        "/images/banner_image.svg"
    };
        protected List<ViewToggleButtons.ViewToggleOption> _viewOptions = new()
    {
        new() { Label = "News", Value = "news" },
        new() { Label = "Business", Value = "business" },
        new() { Label = "Sports", Value = "sports" },
        new() { Label = "Lifestyle", Value = "lifestyle" }
    };
        protected string _selectedView = "news";
        protected string[] Tabs = new[] { "Qatar", "Middle East", "World", "Health & Education", "Community", "Law" };
        protected string SelectedTab = "Qatar";
        [Inject]
        protected NavigationManager navManager { get; set; }
        [Inject] private ILogger<NewsCardBase> Logger { get; set; }
        [Inject] private INewsService _newsService { get; set; }
        protected ContentPost topNews { get; set; } = new ContentPost();
        protected List<ContentPost> moreArticleList { get; set; } = new List<ContentPost>();
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

                if (QatarNewsContent?.QlnNewsNewsQatar?.MoreArticles?.Items != null)
                    moreArticleList = QatarNewsContent.QlnNewsNewsQatar.MoreArticles.Items;

                var topStoryItems = QatarNewsContent?.QlnNewsNewsQatar?.TopStory?.Items;
                if (topStoryItems != null && topStoryItems.Any())
                    topNews = topStoryItems.First();

                isLoading = false;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "OnInitializedAsync");
            }
            finally
            {   
                isLoading = false;
            }   
        }
        protected async void SetViewMode(string view)
        {
            _selectedView = view;
            var selectedCategory = Categories.FirstOrDefault(c => c.Name.Equals(view, StringComparison.OrdinalIgnoreCase));
            if (selectedCategory != null)
            {
                Tabs = selectedCategory.SubCategories.ToArray();
                SelectedTab = Tabs.First();
            }
            else
            {
                Tabs = Array.Empty<string>();
            }
        }
        protected async void SelectTab(string tab)
        {
            isLoading = true;
            SelectedTab = tab;
            switch (tab)
            {

                case "Qatar":
                    QatarNewsContent = await GetNewsQatarAsync();
                    moreArticleList = QatarNewsContent.QlnNewsNewsQatar.MoreArticles.Items;
                    topNews = QatarNewsContent.QlnNewsNewsQatar.TopStory.Items.First();
                    isLoading = false;
                    StateHasChanged();
                    break;
                case "Community":
                    CommunityNewsContent = await GetNewsCommunityAsync();
                    topNews = CommunityNewsContent.QlnNewsNewsCommunity.TopStory.Items.First();
                    moreArticleList = CommunityNewsContent.QlnNewsNewsCommunity.MoreArticles.Items;
                    isLoading = false;
                    StateHasChanged();
                    break;
                case "Law":
                    LawsNewsContent = await GetNewsLawAsync();
                    topNews = LawsNewsContent.QlnNewsNewsLaw.TopStory.Items.First();
                    moreArticleList = LawsNewsContent.QlnNewsNewsLaw.MoreArticles.Items;
                    isLoading = false;
                    StateHasChanged();
                    break;
                case "Health & Education":
                    HealthEducationNewsContent = await GetNewsHealthAndEducationAsync();
                    topNews = HealthEducationNewsContent.QlnNewsNewsHealthEducation.TopStory.Items.First();
                    moreArticleList = HealthEducationNewsContent.QlnNewsNewsHealthEducation.MoreArticles.Items;
                    isLoading = false;
                    StateHasChanged();
                    break;
                case "Middle East":
                    MiddleEastNewsContent = await GetNewsMiddleEastAsync();
                    topNews = MiddleEastNewsContent.QlnNewsNewsMiddleEast.TopStory.Items.First();
                    moreArticleList = MiddleEastNewsContent.QlnNewsNewsMiddleEast.MoreArticles.Items;
                    isLoading = false;
                    StateHasChanged();
                    break;
                case "World":
                    WorldNewsContent = await GetNewsWorldAsync();
                    topNews = WorldNewsContent.QlnNewsNewsWorld.TopStory.Items.First();
                    moreArticleList = WorldNewsContent.QlnNewsNewsWorld.MoreArticles.Items;
                    isLoading = false;
                    StateHasChanged();
                    break;
            }
            isLoading = false;
        }
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
                Console.WriteLine("reached news apii");
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

        protected void onclick(ContentPost news)
        {
            navManager.NavigateTo($"/article/details/{news.Slug}");
        }
    }
}
