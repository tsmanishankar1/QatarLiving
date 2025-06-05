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
        protected string selectedRouterTab = null;
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

        //protected QlnNewsNewsQatarPageResponse QatarNewsContent { get; set; } = new QlnNewsNewsQatarPageResponse();
        //protected QlnNewsFinanceEntrepreneurshipPageResponse FinanceEntrepreneurshipNewsContent { get; set; } = new QlnNewsFinanceEntrepreneurshipPageResponse();
        //protected QlnNewsFinanceRealEstatePageResponse RealEstateNewsContent { get; set; } = new QlnNewsFinanceRealEstatePageResponse();
        //protected QlnNewsFinanceQatarPageResponse FinanceQatarNewsContent { get; set; } = new QlnNewsFinanceQatarPageResponse();
        //protected QlnNewsFinanceMarketUpdatePageResponse MarketUpdateNewsContent { get; set; } = new QlnNewsFinanceMarketUpdatePageResponse();
        //protected QlnNewsFinanceJobsCareersPageResponse JobCareersNewsContent { get; set; } = new QlnNewsFinanceJobsCareersPageResponse();
        //protected QlnNewsFinanceFinancePageResponse FinanceFinanceNewsContent { get; set; } = new QlnNewsFinanceFinancePageResponse();
        //protected QlnNewsNewsCommunityPageResponse CommunityNewsContent { get; set; } = new QlnNewsNewsCommunityPageResponse();
        //protected QlnNewsNewsHealthEducationPageResponse HealthEducationNewsContent { get; set; } = new QlnNewsNewsHealthEducationPageResponse();
        //protected QlnNewsNewsLawPageResponse LawsNewsContent { get; set; } = new QlnNewsNewsLawPageResponse();
        //protected QlnNewsNewsMiddleEastPageResponse MiddleEastNewsContent { get; set; } = new QlnNewsNewsMiddleEastPageResponse();
        //protected QlnNewsNewsWorldPageResponse WorldNewsContent { get; set; } = new QlnNewsNewsWorldPageResponse();
        //protected QlnNewsLifestyleFoodDiningPageResponse FoodDiningNewsContent { get; set; } = new QlnNewsLifestyleFoodDiningPageResponse();
        //protected QlnNewsLifestyleTravelLeisurePageResponse TravelLeisureNewsContent { get; set; } = new QlnNewsLifestyleTravelLeisurePageResponse();
        //protected QlnNewsLifestyleArtsCulturePageResponse ArtsCultureNewsContent { get; set; } = new QlnNewsLifestyleArtsCulturePageResponse();
        //protected QlnNewsLifestyleEventsPageResponse EventsNewsContent { get; set; } = new QlnNewsLifestyleEventsPageResponse();
        //protected QlnNewsLifestyleFashionStylePageResponse FashionNewsContent { get; set; } = new QlnNewsLifestyleFashionStylePageResponse();
        //protected QlnNewsLifestyleHomeLivingPageResponse HomeLivingNewsContent { get; set; } = new QlnNewsLifestyleHomeLivingPageResponse();
        //protected QlnNewsSportsQatarSportsPageResponse QatarSportsNewsContent { get; set; } = new QlnNewsSportsQatarSportsPageResponse();
        //protected QlnNewsSportsFootballPageResponse FoodBallNewsContent { get; set; } = new QlnNewsSportsFootballPageResponse();
        //protected QlnNewsSportsInternationalPageResponse InternationalNewsContent { get; set; } = new QlnNewsSportsInternationalPageResponse();
        //protected QlnNewsSportsMotorsportsPageResponse MotorSportsNewsContent { get; set; } = new QlnNewsSportsMotorsportsPageResponse();
        //protected QlnNewsSportsOlympicsPageResponse OlympicsNewsContent { get; set; } = new QlnNewsSportsOlympicsPageResponse();
        //protected QlnNewsSportsAthleteFeaturesPageResponse AthleteNewsContent { get; set; } = new QlnNewsSportsAthleteFeaturesPageResponse();
        protected GenericNewsPageResponse? NewsContent { get; set; }
        protected List<BannerItem> NewsSideBanners { get; set; } = new();

        protected List<ContentVideo>? VideoList => NewsContent?.News?.WatchOnQatarLiving?.Items;

        protected async override Task OnInitializedAsync()
        {
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
                     if (string.IsNullOrEmpty(localCategory) && !string.IsNullOrEmpty(localSubcategory))
                {
                    localCategory = Categories.FirstOrDefault(cat =>
                        cat.SubCategories.Any(sub => sub.Equals(localSubcategory, StringComparison.OrdinalIgnoreCase)))
                        ?.Name;
                }
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
                    //NewsContent = await GetNewsAsync<GenericNewsPageResponse>("Qatar");

                    //await LoadInitialData();

                    // Top News Slot
                    //var topStoryItems = QatarNewsContent?.News?.TopStory?.Items;
                    //if (topStoryItems != null && topStoryItems.Any())
                    //    topNewsSlot = topStoryItems.First();

                    // Top News Stories Slot
                    // this doesnt appear in the dataset as yet

                    //if (NewsContent?.News?.TopStory?.Items != null) // replace
                    //    topNewsListSlot = NewsContent.News.TopStory.Items; // replace

                    //// More Articles Slot

                    //if (NewsContent?.News?.MoreArticles?.Items != null)
                    //    moreArticleListSlot = NewsContent.News.MoreArticles.Items;

                    //// Most Watched Slot

                    //if (NewsContent?.News?.MostPopularArticles?.Items != null) // replace
                    //    mostWatchedArticleListSlot = new List<ContentVideo>(); // NewsContent.News.MostPopularArticles.Items; //replace

                    //// Articles 1 Slot

                    //if (NewsContent?.News?.Articles1?.Items != null)
                    //    articleListSlot1 = NewsContent.News.Articles1.Items;

                    //// Most Popular Articles Slot

                    //if (NewsContent?.News?.MostPopularArticles?.Items != null)
                    //    popularArticleListSlot = NewsContent.News.MostPopularArticles.Items;

                    //// Articles 2 Slot

                    //if (NewsContent?.News?.Articles2?.Items != null)
                    //    articleListSlot2 = NewsContent.News.Articles2.Items;
                    // if (NewsContent?.News?.WatchOnQatarLiving.Items != null)
                    //     VideoList = NewsContent?.News?.WatchOnQatarLiving.Items;
                }
                //await LoadBanners(SelectedTab);
                //await Task.WhenAll(bannersTask);
                //var videoContent = await GetContentVideoLandingAsync();
                //VideoList = videoContent?.QlnVideos?.QlnVideosTopVideos?.Items ?? [];

                //isLoading = false;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "OnInitializedAsync");
            }
            finally
            {
                //isLoading = false;
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender) return;

            isLoading = true;

            try
            {
                await Task.WhenAll(
                        LoadBanners(SelectedTab),
                        LoadInitialData()
                        );
                //var videoContent = await GetContentVideoLandingAsync();
                //VideoList = videoContent?.QlnVideos?.QlnVideosTopVideos?.Items ?? [];

                //isLoading = false;
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
            if (string.IsNullOrEmpty(selectedRouterTab))
            {
                selectedCategory = Categories.FirstOrDefault(c => c.Name.Equals(view, StringComparison.OrdinalIgnoreCase));
                selectedTabView = _viewOptions.FirstOrDefault(x => x.Value == _selectedView)?.Label;
            }
            else
            {
                Console.WriteLine("the selected tab is the eerf" + selectedRouterTab);
                selectedCategory = Categories.FirstOrDefault(c => c.Name.Equals(view, StringComparison.OrdinalIgnoreCase));
                selectedTabView = _viewOptions.FirstOrDefault(x => x.Value == _selectedView)?.Label;
            }
            
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
        protected void PreSelectCategory()
        {
            // var uri = navManager.ToAbsoluteUri(navManager.Uri);
            // var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);
            // string? localCategory = null;
            // string? localSubcategory = null;
            // if (query.TryGetValue("category", out var cat))
            //     localCategory = cat;

            // if (query.TryGetValue("subcategory", out var sub))
            //     localSubcategory = sub;
            // SetViewMode(localCategory);
            // SelectTab(localSubcategory);
        }
        protected void OnImageLoaded()
        {
            imageLoaded = true;
            StateHasChanged();
        }
        protected override void OnParametersSet()
        {
            imageLoaded = false; 
        }
 
        protected void OnImageError()
        {
            imageLoaded = true;
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
            // var bannersTask = LoadBanners(SelectedTab);
            // topNews = new ContentPost();
            // moreArticleList.Clear();
            NewsContent = await GetNewsAsync<GenericNewsPageResponse>(tab);

//            switch (tab)
//            {
//                case "Qatar":
//                    //NewsContent = await GetNewsAsync<GenericNewsPageResponse>("Qatar");
//                    var qatar = NewsContent?.News;
//                    //topNewsSlot = qatar?.TopStory?.Items?.FirstOrDefault();
//                    topNewsListSlot = qatar?.TopStory?.Items ?? new List<ContentPost>();
//                    moreArticleListSlot = qatar?.MoreArticles?.Items ?? new List<ContentPost>();
//                    mostWatchedArticleListSlot = new List<ContentVideo>();
//                    articleListSlot1 = qatar?.Articles1?.Items ?? new List<ContentPost>();
//                    popularArticleListSlot = qatar?.MostPopularArticles?.Items ?? new List<ContentPost>();
//                    articleListSlot2 = qatar?.Articles2?.Items ?? new List<ContentPost>();
//                    VideoList = qatar?.WatchOnQatarLiving?.Items ?? new List<ContentVideo>();
//                    break;

//                case "Community":
//                    NewsContent = await GetNewsAsync<QlnNewsNewsCommunityPageResponse>("Community");
//                    var community = CommunityNewsContent?.News;
//                    //topNewsSlot = community?.TopStory?.Items?.FirstOrDefault();
//                    topNewsListSlot = community?.TopStory?.Items ?? new List<ContentPost>();
//                    moreArticleListSlot = community?.MoreArticles?.Items ?? new List<ContentPost>();
//                    mostWatchedArticleListSlot = new List<ContentVideo>();
//                    articleListSlot1 = community?.Articles1?.Items ?? new List<ContentPost>();
//                    popularArticleListSlot = community?.MostPopularArticles?.Items ?? new List<ContentPost>();
//                    articleListSlot2 = community?.Articles2?.Items ?? new List<ContentPost>();
//                    VideoList = community?.WatchOnQatarLiving?.Items ?? new List<ContentVideo>();
//                    if (VideoList != null && VideoList.Any())
//{
//    foreach (var video in VideoList)
//    {
//        Console.WriteLine($"Nid: {video.Nid}, Title: {video.Title}, User: {video.UserName}, Video URL: {video.VideoUrl}");
//    }
//}
                    
//                    break;

//                case "Law":
//                    NewsContent = await GetNewsAsync<QlnNewsNewsLawPageResponse>("Law");
//                    var law = LawsNewsContent?.News;
//                    //topNewsSlot = law?.TopStory?.Items?.FirstOrDefault();
//                    topNewsListSlot = law?.TopStory?.Items ?? new List<ContentPost>();
//                    moreArticleListSlot = law?.MoreArticles.Items ?? new List<ContentPost>();
//                    mostWatchedArticleListSlot = new List<ContentVideo>();
//                    articleListSlot1 = law?.Articles1?.Items ?? new List<ContentPost>();
//                    popularArticleListSlot = law?.MostPopularArticles?.Items ?? new List<ContentPost>();
//                    articleListSlot2 = law?.Articles2?.Items ?? new List<ContentPost>();
//                    VideoList = law?.WatchOnQatarLiving?.Items ?? new List<ContentVideo>();
//                    break;

//                case "Health & Education":
//                    NewsContent = await GetNewsAsync<QlnNewsNewsHealthEducationPageResponse>("Health & Education");
//                    var health = HealthEducationNewsContent?.News;
//                    //topNewsSlot = health?.TopStory?.Items?.FirstOrDefault();
//                    topNewsListSlot = health?.TopStory?.Items ?? new List<ContentPost>();
//                    moreArticleListSlot = health?.MoreArticles?.Items ?? new List<ContentPost>();
//                    mostWatchedArticleListSlot = new List<ContentVideo>();
//                    articleListSlot1 = health?.Articles1?.Items ?? new List<ContentPost>();
//                    popularArticleListSlot = health?.MostPopularArticles?.Items ?? new List<ContentPost>();
//                    articleListSlot2 = health?.Articles2?.Items ?? new List<ContentPost>();
//                    VideoList = health?.WatchOnQatarLiving?.Items ?? new List<ContentVideo>();
//                    break;

//                case "Middle East":
//                    NewsContent = await GetNewsAsync<QlnNewsNewsMiddleEastPageResponse>("Middle East");
//                    var middleEast = MiddleEastNewsContent?.News;
//                    //topNewsSlot = middleEast?.TopStory?.Items?.FirstOrDefault();
//                    topNewsListSlot = middleEast?.TopStory?.Items ?? new List<ContentPost>();
//                    moreArticleListSlot = middleEast?.MoreArticles?.Items ?? new List<ContentPost>();
//                    mostWatchedArticleListSlot = new List<ContentVideo>();
//                    articleListSlot1 = middleEast?.Articles1?.Items ?? new List<ContentPost>();
//                    popularArticleListSlot = middleEast?.MostPopularArticles?.Items ?? new List<ContentPost>();
//                    articleListSlot2 = middleEast?.Articles2?.Items ?? new List<ContentPost>();
//                    VideoList = middleEast?.WatchOnQatarLiving?.Items ?? new List<ContentVideo>();
//                    break;

//                case "Qatar Economy":
//                    NewsContent = await GetNewsAsync<QlnNewsFinanceQatarPageResponse>("Qatar Economy");
//                    var economy = FinanceQatarNewsContent?.News;
//                    //topNewsSlot = economy?.TopStory?.Items?.FirstOrDefault();
//                    topNewsListSlot = economy?.TopStory?.Items ?? new List<ContentPost>();
//                    moreArticleListSlot = economy?.MoreArticles?.Items ?? new List<ContentPost>();
//                    mostWatchedArticleListSlot = new List<ContentVideo>();
//                    articleListSlot1 = economy?.Articles1?.Items ?? new List<ContentPost>();
//                    popularArticleListSlot = economy?.MostPopularArticles?.Items ?? new List<ContentPost>();
//                    articleListSlot2 = economy?.Articles2?.Items ?? new List<ContentPost>();
//                    VideoList = economy?.WatchOnQatarLiving?.Items ?? new List<ContentVideo>();
//                    break;

//                case "World":
//                    NewsContent = await GetNewsAsync<QlnNewsNewsWorldPageResponse>("World");
//                    var world = WorldNewsContent?.News;
//                    //topNewsSlot = world?.TopStory?.Items?.FirstOrDefault();
//                    topNewsListSlot = world?.TopStory?.Items ?? new List<ContentPost>();
//                    moreArticleListSlot = world?.MoreArticles?.Items ?? new List<ContentPost>();
//                    mostWatchedArticleListSlot = new List<ContentVideo>();
//                    articleListSlot1 = world?.Articles1?.Items ?? new List<ContentPost>();
//                    popularArticleListSlot = world?.MostPopularArticles?.Items ?? new List<ContentPost>();
//                    articleListSlot2 = world?.Articles2?.Items ?? new List<ContentPost>();
//                    VideoList = world?.WatchOnQatarLiving?.Items ?? new List<ContentVideo>();
//                    break;
//                case "Market Updates":
//                    NewsContent = await GetNewsAsync<QlnNewsFinanceMarketUpdatePageResponse>("Market Updates");
//                    var market = MarketUpdateNewsContent?.News;
//                    //topNewsSlot = market?.TopStory?.Items?.FirstOrDefault();
//                    topNewsListSlot = market?.TopStory?.Items ?? new List<ContentPost>();
//                    moreArticleListSlot = market?.MoreArticles?.Items ?? new List<ContentPost>();
//                    mostWatchedArticleListSlot = new List<ContentVideo>();
//                    articleListSlot1 = market?.Articles1?.Items ?? new List<ContentPost>();
//                    popularArticleListSlot = market?.MostPopularArticles?.Items ?? new List<ContentPost>();
//                    articleListSlot2 = market?.Articles2?.Items ?? new List<ContentPost>();
//                    VideoList = market?.WatchOnQatarLiving?.Items ?? new List<ContentVideo>();
//                    break;
//                case "Real Estate":
//                    NewsContent = await GetNewsAsync<QlnNewsFinanceRealEstatePageResponse>("Real Estate");
//                    var realEstate = RealEstateNewsContent?.News;
//                    //topNewsSlot = realEstate?.TopStory?.Items?.FirstOrDefault();
//                    topNewsListSlot = realEstate?.TopStory?.Items ?? new List<ContentPost>();
//                    moreArticleListSlot = realEstate?.MoreArticles?.Items ?? new List<ContentPost>();
//                    mostWatchedArticleListSlot = new List<ContentVideo>();
//                    articleListSlot1 = realEstate?.Articles1?.Items ?? new List<ContentPost>();
//                    popularArticleListSlot = realEstate?.MostPopularArticles?.Items ?? new List<ContentPost>();
//                    articleListSlot2 = realEstate?.Articles2?.Items ?? new List<ContentPost>();
//                    VideoList = realEstate?.WatchOnQatarLiving?.Items ?? new List<ContentVideo>();
//                    break;
//                case "Entrepreneurship":
//                    NewsContent = await GetNewsAsync<QlnNewsFinanceEntrepreneurshipPageResponse>("Entrepreneurship");
//                    var entrepreneurship = FinanceEntrepreneurshipNewsContent?.News;
//                    //topNewsSlot = entrepreneurship?.TopStory?.Items?.FirstOrDefault();
//                    topNewsListSlot = entrepreneurship?.TopStory?.Items ?? new List<ContentPost>();
//                    moreArticleListSlot = entrepreneurship?.MoreArticles?.Items ?? new List<ContentPost>();
//                    mostWatchedArticleListSlot = new List<ContentVideo>();
//                    articleListSlot1 = entrepreneurship?.Articles1?.Items ?? new List<ContentPost>();
//                    popularArticleListSlot = entrepreneurship?.MostPopularArticles?.Items ?? new List<ContentPost>();
//                    articleListSlot2 = entrepreneurship?.Articles2?.Items ?? new List<ContentPost>();
//                    VideoList = entrepreneurship?.WatchOnQatarLiving?.Items ?? new List<ContentVideo>();
//                    break;
//                case "Finance":
//                    NewsContent = await GetNewsAsync<QlnNewsFinanceFinancePageResponse>("Finance");
//                    var finance = FinanceFinanceNewsContent?.News;
//                    //topNewsSlot = finance?.TopStory?.Items?.FirstOrDefault();
//                    topNewsListSlot = finance?.TopStory?.Items ?? new List<ContentPost>();
//                    moreArticleListSlot = finance?.MoreArticles?.Items ?? new List<ContentPost>();
//                    mostWatchedArticleListSlot = new List<ContentVideo>();
//                    articleListSlot1 = finance?.Articles1?.Items ?? new List<ContentPost>();
//                    popularArticleListSlot = finance?.MostPopularArticles?.Items ?? new List<ContentPost>();
//                    articleListSlot2 = finance?.Articles2?.Items ?? new List<ContentPost>();
//                    VideoList = finance?.WatchOnQatarLiving?.Items ?? new List<ContentVideo>();
//                    break;
//                case "Jobs & Careers":
//                    NewsContent = await GetNewsAsync<QlnNewsFinanceJobsCareersPageResponse>("Jobs & Careers");
//                    var jobs = JobCareersNewsContent?.News;
//                    //topNewsSlot = jobs?.TopStory?.Items?.FirstOrDefault();
//                    topNewsListSlot = jobs?.TopStory?.Items ?? new List<ContentPost>();
//                    moreArticleListSlot = jobs?.MoreArticles?.Items ?? new List<ContentPost>();
//                    mostWatchedArticleListSlot = new List<ContentVideo>();
//                    articleListSlot1 = jobs?.Articles1?.Items ?? new List<ContentPost>();
//                    popularArticleListSlot = jobs?.MostPopularArticles?.Items ?? new List<ContentPost>();
//                    articleListSlot2 = jobs?.Articles2?.Items ?? new List<ContentPost>();
//                    // VideoList = jobs?.WatchOnQatarLiving?.Items ?? new List<ContentVideo>();
//                    break;
//                case "Food & Dining":
//                    NewsContent = await GetNewsAsync<QlnNewsLifestyleFoodDiningPageResponse>("Food & Dining");
//                    var foods = FoodDiningNewsContent?.News;
//                    //topNewsSlot = foods?.TopStory?.Items?.FirstOrDefault();
//                    topNewsListSlot = foods?.TopStory?.Items ?? new List<ContentPost>();
//                    moreArticleListSlot = foods?.MoreArticles?.Items ?? new List<ContentPost>();
//                    mostWatchedArticleListSlot = new List<ContentVideo>();
//                    articleListSlot1 = foods?.Articles1?.Items ?? new List<ContentPost>();
//                    popularArticleListSlot = foods?.MostPopularArticles?.Items ?? new List<ContentPost>();
//                    articleListSlot2 = foods?.Articles2?.Items ?? new List<ContentPost>();
//                    VideoList = foods?.WatchOnQatarLiving?.Items ?? new List<ContentVideo>();
//                    if (VideoList != null && VideoList.Any())
//{
//    foreach (var video in VideoList)
//    {
//        Console.WriteLine($"Nid: {video.Nid}, Title: {video.Title}, User: {video.UserName}, Video URL: {video.VideoUrl}");
//    }
//}
//                    break;
//                case "Travel & Leisure":
//                    NewsContent = await GetNewsAsync<QlnNewsLifestyleTravelLeisurePageResponse>("Travel & Leisure");
//                    var travel = TravelLeisureNewsContent?.News;
//                    //topNewsSlot = travel?.TopStory?.Items?.FirstOrDefault();
//                    topNewsListSlot = travel?.TopStory?.Items ?? new List<ContentPost>();
//                    moreArticleListSlot = travel?.MoreArticles?.Items ?? new List<ContentPost>();
//                    mostWatchedArticleListSlot = new List<ContentVideo>();
//                    articleListSlot1 = travel?.Articles1?.Items ?? new List<ContentPost>();
//                    popularArticleListSlot = travel?.MostPopularArticles?.Items ?? new List<ContentPost>();
//                    articleListSlot2 = travel?.Articles2?.Items ?? new List<ContentPost>();
//                    VideoList = travel?.WatchOnQatarLiving?.Items ?? new List<ContentVideo>();
//                    break;
//                case "Arts & Culture":
//                    NewsContent = await GetNewsAsync<QlnNewsLifestyleArtsCulturePageResponse>("Arts & Culture");
//                    var arts = ArtsCultureNewsContent?.News;
//                    //topNewsSlot = arts?.TopStory?.Items?.FirstOrDefault();
//                    topNewsListSlot = arts?.TopStory?.Items ?? new List<ContentPost>();
//                    moreArticleListSlot = arts?.MoreArticles?.Items ?? new List<ContentPost>();
//                    mostWatchedArticleListSlot = new List<ContentVideo>();
//                    articleListSlot1 = arts?.Articles1?.Items ?? new List<ContentPost>();
//                    popularArticleListSlot = arts?.MostPopularArticles?.Items ?? new List<ContentPost>();
//                    articleListSlot2 = arts?.Articles2?.Items ?? new List<ContentPost>();
//                    VideoList = arts?.WatchOnQatarLiving?.Items ?? new List<ContentVideo>();
//                    break;
//                case "Events":
//                    NewsContent = await GetNewsAsync<QlnNewsLifestyleEventsPageResponse>("Events");
//                    var events = EventsNewsContent?.News;
//                    //topNewsSlot = events?.TopStory?.Items?.FirstOrDefault();
//                    topNewsListSlot = events?.MoreArticles?.Items ?? new List<ContentPost>();
//                    moreArticleListSlot = events?.MoreArticles?.Items ?? new List<ContentPost>();
//                    mostWatchedArticleListSlot = new List<ContentVideo>();
//                    articleListSlot1 = events?.Articles1?.Items ?? new List<ContentPost>();
//                    popularArticleListSlot = events?.MostPopularArticles?.Items ?? new List<ContentPost>();
//                    articleListSlot2 = events?.Articles2?.Items ?? new List<ContentPost>();
//                    VideoList = events?.WatchOnQatarLiving?.Items ?? new List<ContentVideo>();
//                    break;
//                case "Fashion & Style":
//                    NewsContent = await GetNewsAsync<QlnNewsLifestyleFashionStylePageResponse>("Fashion & Style");
//                    var fashion = FashionNewsContent?.News;
//                    //topNewsSlot = fashion?.TopStory?.Items?.FirstOrDefault();
//                    topNewsListSlot = fashion?.TopStory?.Items ?? new List<ContentPost>();
//                    moreArticleListSlot = fashion?.MoreArticles?.Items ?? new List<ContentPost>();
//                    mostWatchedArticleListSlot = new List<ContentVideo>();
//                    articleListSlot1 = fashion?.Articles1?.Items ?? new List<ContentPost>();
//                    popularArticleListSlot = fashion?.MostPopularArticles?.Items ?? new List<ContentPost>();
//                    articleListSlot2 = fashion?.Articles2?.Items ?? new List<ContentPost>();
//                    VideoList = fashion?.WatchOnQatarLiving?.Items ?? new List<ContentVideo>();
//                    break;
//                case "Home & Living":
//                    NewsContent = await GetNewsAsync<QlnNewsLifestyleHomeLivingPageResponse>("Home & Living");
//                    var home = HomeLivingNewsContent?.News;
//                    //topNewsSlot = home?.TopStory?.Items?.FirstOrDefault();
//                    topNewsListSlot = home?.TopStory?.Items ?? new List<ContentPost>();
//                    moreArticleListSlot = home?.MoreArticles?.Items ?? new List<ContentPost>();
//                    mostWatchedArticleListSlot = new List<ContentVideo>();
//                    articleListSlot1 = home?.Articles1?.Items ?? new List<ContentPost>();
//                    popularArticleListSlot = home?.MostPopularArticles?.Items ?? new List<ContentPost>();
//                    articleListSlot2 = home?.Articles2?.Items ?? new List<ContentPost>();
//                    VideoList = home?.WatchOnQatarLiving?.Items ?? new List<ContentVideo>();
//                    break;
//                case "Qatar Sports":
//                    NewsContent = await GetNewsAsync<QlnNewsSportsQatarSportsPageResponse>("Qatar Sports");
//                    var sports = QatarSportsNewsContent?.News;
//                    //topNewsSlot = sports?.TopStory?.Items?.FirstOrDefault();
//                    topNewsListSlot = sports?.TopStory?.Items ?? new List<ContentPost>();
//                    moreArticleListSlot = sports?.MoreArticles?.Items ?? new List<ContentPost>();
//                    mostWatchedArticleListSlot = new List<ContentVideo>();
//                    articleListSlot1 = sports?.Articles1?.Items ?? new List<ContentPost>();
//                    popularArticleListSlot = sports?.MostPopularArticles?.Items ?? new List<ContentPost>();
//                    articleListSlot2 = sports?.Articles2?.Items ?? new List<ContentPost>();
//                    VideoList = sports?.WatchOnQatarLiving?.Items ?? new List<ContentVideo>();
//                    break;
//                case "Football":
//                    NewsContent = await GetNewsAsync<QlnNewsSportsFootballPageResponse>("Football");
//                    var football = FoodBallNewsContent?.News;
//                    //topNewsSlot = football?.TopStory?.Items?.FirstOrDefault();
//                    topNewsListSlot = football?.TopStory?.Items ?? new List<ContentPost>();
//                    moreArticleListSlot = football?.MoreArticles?.Items ?? new List<ContentPost>();
//                    mostWatchedArticleListSlot = new List<ContentVideo>();
//                    articleListSlot1 = football?.Articles1?.Items ?? new List<ContentPost>();
//                    popularArticleListSlot = football?.MostPopularArticles?.Items ?? new List<ContentPost>();
//                    articleListSlot2 = football?.Articles2?.Items ?? new List<ContentPost>();
//                    VideoList = football?.WatchOnQatarLiving?.Items ?? new List<ContentVideo>();
//                    break;
//                case "International":
//                    NewsContent = await GetNewsAsync<QlnNewsSportsInternationalPageResponse>("International");
//                    var international = InternationalNewsContent?.News;
//                    //topNewsSlot = international?.TopStory?.Items?.FirstOrDefault();
//                    topNewsListSlot = international?.TopStory?.Items ?? new List<ContentPost>();
//                    moreArticleListSlot = international?.MoreArticles?.Items ?? new List<ContentPost>();
//                    mostWatchedArticleListSlot = new List<ContentVideo>();
//                    articleListSlot1 = international?.Articles1?.Items ?? new List<ContentPost>();
//                    popularArticleListSlot = international?.MostPopularArticles?.Items ?? new List<ContentPost>();
//                    articleListSlot2 = international?.Articles2?.Items ?? new List<ContentPost>();
//                    VideoList = international?.WatchOnQatarLiving?.Items ?? new List<ContentVideo>();
//                    break;
//                case "Motorsports":
//                    NewsContent = await GetNewsAsync<QlnNewsSportsMotorsportsPageResponse>("Motorsports");
//                    var motorSports = MotorSportsNewsContent?.News;
//                    //topNewsSlot = motorSports?.TopStory?.Items?.FirstOrDefault();
//                    topNewsListSlot = motorSports?.TopStory?.Items ?? new List<ContentPost>();
//                    moreArticleListSlot = motorSports?.MoreArticles?.Items ?? new List<ContentPost>();
//                    mostWatchedArticleListSlot = new List<ContentVideo>();
//                    articleListSlot1 = motorSports?.Articles1?.Items ?? new List<ContentPost>();
//                    popularArticleListSlot = motorSports?.MostPopularArticles?.Items ?? new List<ContentPost>();
//                    articleListSlot2 = motorSports?.Articles2?.Items ?? new List<ContentPost>();
//                    VideoList = motorSports?.WatchOnQatarLiving?.Items ?? new List<ContentVideo>();
//                    break;
//                case "Olympics":
//                    NewsContent = await GetNewsAsync<QlnNewsSportsOlympicsPageResponse>("Olympics");
//                    var olympics = OlympicsNewsContent?.News;
//                    //topNewsSlot = olympics?.TopStory?.Items?.FirstOrDefault();
//                    topNewsListSlot = olympics?.TopStory?.Items ?? new List<ContentPost>();
//                    moreArticleListSlot = olympics?.MoreArticles?.Items ?? new List<ContentPost>();
//                    mostWatchedArticleListSlot = new List<ContentVideo>();
//                    articleListSlot1 = olympics?.Articles1?.Items ?? new List<ContentPost>();
//                    popularArticleListSlot = olympics?.MostPopularArticles?.Items ?? new List<ContentPost>();
//                    articleListSlot2 = olympics?.Articles2?.Items ?? new List<ContentPost>();
//                    VideoList = olympics?.WatchOnQatarLiving?.Items ?? new List<ContentVideo>();
//                    break;
//                case "Athlete Features":
//                    NewsContent = await GetNewsAsync<QlnNewsSportsAthleteFeaturesPageResponse>("Athlete Features");
//                    var athelete = AthleteNewsContent?.News;
//                    //topNewsSlot = athelete?.TopStory?.Items?.FirstOrDefault();
//                    topNewsListSlot = athelete?.TopStory?.Items ?? new List<ContentPost>();
//                    moreArticleListSlot = athelete?.MoreArticles?.Items ?? new List<ContentPost>();
//                    mostWatchedArticleListSlot = new List<ContentVideo>();
//                    articleListSlot1 = athelete?.Articles1?.Items ?? new List<ContentPost>();
//                    popularArticleListSlot = athelete?.MostPopularArticles?.Items ?? new List<ContentPost>();
//                    articleListSlot2 = athelete?.Articles2?.Items ?? new List<ContentPost>();
//                    VideoList = athelete?.WatchOnQatarLiving?.Items ?? new List<ContentVideo>();
//                    break;
//            }
            navManager.NavigateTo($"/content/news?category={selectedTabView}&subcategory={SelectedTab}", forceLoad: false);

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
        //protected async Task<QlnNewsNewsQatarPageResponse> GetNewsQatarAsync()
        //{
        //    try
        //    {
        //        var apiResponse = await _newsService.GetNewsQatarAsync() ?? new HttpResponseMessage();
        //        if (apiResponse.IsSuccessStatusCode && apiResponse.Content != null)
        //        {
        //            var response = await apiResponse.Content.ReadFromJsonAsync<QlnNewsNewsQatarPageResponse>();
        //            return response ?? new QlnNewsNewsQatarPageResponse();
        //        }
        //        return new QlnNewsNewsQatarPageResponse();
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.LogError(ex, "GetNewsQatarAsync");
        //        return new QlnNewsNewsQatarPageResponse();
        //    }
        //}
        //protected async Task<QlnNewsNewsCommunityPageResponse> GetNewsCommunityAsync()
        //{
        //    try
        //    {
        //        var apiResponse = await _newsService.GetNewsCommunityAsync() ?? new HttpResponseMessage();

        //        if (apiResponse.IsSuccessStatusCode && apiResponse.Content != null)
        //        {
        //            var response = await apiResponse.Content.ReadFromJsonAsync<QlnNewsNewsCommunityPageResponse>();
        //            return response ?? new QlnNewsNewsCommunityPageResponse();
        //        }
        //        return new QlnNewsNewsCommunityPageResponse();
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.LogError(ex, "GetNewsCommunityAsync");
        //        return new QlnNewsNewsCommunityPageResponse();
        //    }
        //}
        //protected async Task<QlnNewsNewsHealthEducationPageResponse> GetNewsHealthAndEducationAsync()
        //{
        //    try
        //    {
        //        var apiResponse = await _newsService.GetNewsHealthAndEducationAsync() ?? new HttpResponseMessage();

        //        if (apiResponse.IsSuccessStatusCode && apiResponse.Content != null)
        //        {
        //            var response = await apiResponse.Content.ReadFromJsonAsync<QlnNewsNewsHealthEducationPageResponse>();
        //            return response ?? new QlnNewsNewsHealthEducationPageResponse();
        //        }
        //        return new QlnNewsNewsHealthEducationPageResponse();
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.LogError(ex, "GetNewsHealthAndEducationAsync");
        //        return new QlnNewsNewsHealthEducationPageResponse();
        //    }
        //}
        //protected async Task<QlnNewsNewsLawPageResponse> GetNewsLawAsync()
        //{
        //    try
        //    {
        //        var apiResponse = await _newsService.GetNewsLawAsync() ?? new HttpResponseMessage();

        //        if (apiResponse.IsSuccessStatusCode && apiResponse.Content != null)
        //        {
        //            var response = await apiResponse.Content.ReadFromJsonAsync<QlnNewsNewsLawPageResponse>();
        //            return response ?? new QlnNewsNewsLawPageResponse();
        //        }
        //        return new QlnNewsNewsLawPageResponse();
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.LogError(ex, "GGetNewsLawAsync");
        //        return new QlnNewsNewsLawPageResponse();
        //    }
        //}
        //protected async Task<QlnNewsNewsMiddleEastPageResponse> GetNewsMiddleEastAsync()
        //{
        //    try
        //    {
        //        var apiResponse = await _newsService.GetNewsMiddleEastAsync() ?? new HttpResponseMessage();
        //        if (apiResponse.IsSuccessStatusCode && apiResponse.Content != null)
        //        {
        //            var response = await apiResponse.Content.ReadFromJsonAsync<QlnNewsNewsMiddleEastPageResponse>();
        //            return response ?? new QlnNewsNewsMiddleEastPageResponse();
        //        }
        //        return new QlnNewsNewsMiddleEastPageResponse();
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.LogError(ex, "GetNewsMiddleEastAsync");
        //        return new QlnNewsNewsMiddleEastPageResponse();
        //    }
        //}
        //protected async Task<QlnNewsNewsWorldPageResponse> GetNewsWorldAsync()
        //{
        //    try
        //    {
        //        var apiResponse = await _newsService.GetNewsWorldAsync() ?? new HttpResponseMessage();

        //        if (apiResponse.IsSuccessStatusCode && apiResponse.Content != null)
        //        {
        //            var response = await apiResponse.Content.ReadFromJsonAsync<QlnNewsNewsWorldPageResponse>();
        //            return response ?? new QlnNewsNewsWorldPageResponse();
        //        }
        //        return new QlnNewsNewsWorldPageResponse();
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.LogError(ex, "GetNewsWorldAsync");
        //        return new QlnNewsNewsWorldPageResponse();
        //    }
        //}

        protected void onclick(ContentPost news)
        {
            if (!string.IsNullOrEmpty(selectedTabView) && !string.IsNullOrEmpty(SelectedTab))
            {
                // navManager.NavigateTo($"/content/article/details/{selectedTabView}/{SelectedTab}/{news.Slug}");
                navManager.NavigateTo($"/content/article/details/{news.Slug}?category={selectedTabView}&subcategory={SelectedTab}");
            }
            else if (!string.IsNullOrEmpty(selectedTabView))
            {
                // navManager.NavigateTo($"/content/article/details/{selectedTabView}/{news.Slug}");
                navManager.NavigateTo($"/content/article/details/{news.Slug}?category={selectedTabView}");
            }
            else
            {
                // navManager.NavigateTo($"/content/article/details/{news.Slug}");
                navManager.NavigateTo($"/content/article/details/{news.Slug}");
            }
        }


        /// <summary>
        /// Gets Content Videos Page data
        /// </summary>
        /// <returns>ContentsVideosResponse</returns>
        //protected async Task<ContentsVideosResponse> GetContentVideoLandingAsync()
        //{
        //    try
        //    {
        //        var apiResponse = await _contentService.GetVideosLPAsync() ?? new HttpResponseMessage();

        //        if (apiResponse.IsSuccessStatusCode && apiResponse.Content != null)
        //        {
        //            var response = await apiResponse.Content.ReadFromJsonAsync<ContentsVideosResponse>();
        //            return response ?? new ContentsVideosResponse();
        //        }

        //        return new ContentsVideosResponse();
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.Message, "GetContentVideoLandingAsync");
        //        return new ContentsVideosResponse();
        //    }
        //}
    }
}


