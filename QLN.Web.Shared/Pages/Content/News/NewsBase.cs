using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Web.Shared.Components.ViewToggleButtons;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Services.Interface;
using System.Net.Http.Json;
using System.Text.Json;

namespace QLN.Web.Shared.Pages.Content.News
{
    public class NewsBase : ComponentBase
    {
        [Inject] IContentService _contentService { get; set; }
        public bool isLoading = true;
        protected bool isLoadingBanners = true;
        protected bool imageFailed = false;
        protected string? currentImageUrl;
        protected List<BannerItem> DailyTakeOverBanners = new();
        protected bool imageLoaded = false;
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
    protected List<ViewToggleButtons.ViewToggleOption> routerList = new()
    {   
    new() { Label = "News", Value = "news" },
    new() { Label = "Qatar", Value = "qatar" },
    new() { Label = "Middle East", Value = "middleeast" },
    new() { Label = "World", Value = "world" },
    new() { Label = "Health & Education", Value = "health-and-education" },
    new() { Label = "Community", Value = "community" },
    new() { Label = "Law", Value = "law" },

    new() { Label = "Business", Value = "business" },
    new() { Label = "Qatar Economy", Value = "qatar-economy" },
    new() { Label = "Market Updates", Value = "market-update" },
    new() { Label = "Real Estate", Value = "real-estate" },
    new() { Label = "Entrepreneurship", Value = "entrepreneurship" },
    new() { Label = "Finance", Value = "finance" },
    new() { Label = "Jobs & Careers", Value = "jobs-and-careers" },

    new() { Label = "Sports", Value = "sports" },
    new() { Label = "Qatar Sports", Value = "qatar-sports" },
    new() { Label = "Football", Value = "football" },
    new() { Label = "International", Value = "international" },
    new() { Label = "Motorsports", Value = "motorsports" },
    new() { Label = "Olympics", Value = "olympics" },
    new() { Label = "Athlete Features", Value = "athlete-features" },

    new() { Label = "Lifestyle", Value = "lifestyle" },
    new() { Label = "Food & Dining", Value = "food-dining" },
    new() { Label = "Travel & Leisure", Value = "travel-leisure" },
    new() { Label = "Arts & Culture", Value = "arts-and-culture" },
    new() { Label = "Events", Value = "events" },
    new() { Label = "Fashion & Style", Value = "fashion-and-style" },
    new() { Label = "Home & Living", Value = "home-and-living" }
};
        protected string _selectedView = "news";
        protected string selectedTabView = "News";
        protected string[] Tabs = new[] { "Qatar", "Middle East", "World", "Health & Education", "Community", "Law" };
        protected string SelectedTab = "Qatar";
        protected string subTabLabel = "qatar";
        protected string selectedRouterTab;
        protected List<BannerItem> DailyHeroBanners { get; set; } = new();
        [Inject]
        protected NavigationManager navManager { get; set; }
        [Inject] private ILogger<NewsCardBase> Logger { get; set; }
        [Inject] private INewsService _newsService { get; set; }
        [Inject] private IEventService _eventService { get; set; }
        [Inject] private ISimpleMemoryCache _simpleCacheService { get; set; }
        protected ContentPost? topNewsSlot => NewsContent?.News?.TopStory?.Items?.FirstOrDefault();
        protected List<ContentPost>? topNewsListSlot => NewsContent?.News?.TopStory?.Items;
        protected List<ContentPost>? moreArticleListSlot => NewsContent?.News?.MoreArticles?.Items;
        protected List<ContentVideo> mostWatchedArticleListSlot { get; set; } = new List<ContentVideo>();
        protected List<ContentPost>? articleListSlot1 => NewsContent?.News?.Articles1?.Items; // making this an event slot for now
        protected List<ContentPost>? popularArticleListSlot => NewsContent?.News?.MostPopularArticles?.Items;
        protected List<ContentPost>? articleListSlot2 => NewsContent?.News?.Articles2?.Items; // making this an event slot for now
        protected GenericNewsPageResponse? NewsContent { get; set; }
        protected List<BannerItem> NewsSideBanners { get; set; } = new();

        protected List<ContentVideo>? VideoList => NewsContent?.News?.WatchOnQatarLiving?.Items;

        protected async override Task OnInitializedAsync()
        {
            isLoading = true;
            if (currentImageUrl != topNewsSlot?.ImageUrl)
        {
            currentImageUrl = topNewsSlot?.ImageUrl;
            imageLoaded = false;
            imageFailed = false;
        }
            try
            {
                var uri = navManager.ToAbsoluteUri(navManager.Uri);
                var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);
                string? localCategory = null;
                string? localSubcategory = null;
                if (query.TryGetValue("category", out var cat))
                    localCategory = cat;

                if (query.TryGetValue("subcategory", out var sub))
                    localSubcategory = sub;
                if (!string.IsNullOrEmpty(localCategory))
                {
                    var routeName = routerList.FirstOrDefault(item => item.Value == localSubcategory)?.Label;
                    if (!string.IsNullOrEmpty(localSubcategory))
                    {
                        selectedRouterTab = routeName;
                    }
                    SetViewMode(localCategory);
                }
                else
                {
                    await LoadInitialData();
                }
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

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender) return;

            try
            {
                isLoading = true;
                StateHasChanged();
                await LoadBanners(SelectedTab);
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

        private async Task LoadInitialData()
        {
            NewsContent = await GetNewsAsync<GenericNewsPageResponse>("Qatar");
            StateHasChanged();
        }

        protected async void SetViewMode(string view)
        {
            _selectedView = view;
            Category selectedCategory;
            selectedTabView = _viewOptions.FirstOrDefault(x => x.Value == _selectedView)?.Label;
            selectedCategory = Categories.FirstOrDefault(c => c.Name.Equals(view, StringComparison.OrdinalIgnoreCase));
            if (selectedCategory != null)
            {
                Tabs = selectedCategory.SubCategories.ToArray();
                if (string.IsNullOrEmpty(selectedRouterTab))
                {
                    SelectTab(Tabs.First());
                }
                else
                {
                    SelectTab(selectedRouterTab);
                }
            }
            else
            {
                    Tabs = Array.Empty<string>();
                }
            
        }
        protected void OnImageLoaded()
        {
            imageLoaded = true;
            imageFailed = false;
            StateHasChanged();
        }
        protected override void OnParametersSet()
        {
            imageLoaded = false; 
        }
 
        protected void OnImageError()
        {
            imageLoaded = true; 
            imageFailed = true; 
            StateHasChanged();
        }
        protected async Task LoadBanners(string tab)
        {
            isLoadingBanners = true;
            try
            {
                var banners = await _simpleCacheService.GetBannerAsync();
                NewsSideBanners = banners?.ContentNewsSide ?? new List<BannerItem>();
                DailyHeroBanners = banners?.ContentNewsHero ?? new List<BannerItem>();
                DailyTakeOverBanners = banners?.ContentNewsTakeover ?? new List<BannerItem>();
            }
            finally
            {
                isLoadingBanners = false;
            }
        }
        
        protected async void SelectTab(string tab)
        {
            isLoading = true;
            SelectedTab = tab;
            NewsContent = await GetNewsAsync<GenericNewsPageResponse>(tab);
            subTabLabel = routerList.FirstOrDefault(item => item.Label == SelectedTab)?.Value;
            navManager.NavigateTo($"/content/news?category={_selectedView}&subcategory={subTabLabel}", forceLoad: false);
            selectedRouterTab = string.Empty;
            isLoading = false;
            StateHasChanged();
        }
       
         protected async Task<T> GetNewsAsync<T>(string tab) where T : new()
        {
            try
            {
                var apiResponse = await _newsService.GetNewsAsync(tab) ?? new HttpResponseMessage();
                if (apiResponse.IsSuccessStatusCode && apiResponse.Content != null)
                {
                    var response = await apiResponse.Content.ReadFromJsonAsync<T>();
                    return response ?? new T();
                }
                return new T();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"GetNewsAsync<{typeof(T).Name}> failed for tab: {tab}");
                return new T();
            }
        }
        protected void onclick(ContentPost news)
        {
            if (!string.IsNullOrEmpty(_selectedView) && !string.IsNullOrEmpty(subTabLabel))
            {
                navManager.NavigateTo($"/content/article/details/{news.Slug}?category={_selectedView}&subcategory={subTabLabel}");
            }
            else if (!string.IsNullOrEmpty(_selectedView))
            {
                navManager.NavigateTo($"/content/article/details/{news.Slug}?category={_selectedView}");
            }
            else
            {
                navManager.NavigateTo($"/content/article/details/{news.Slug}");
            }
        }
    }
}


