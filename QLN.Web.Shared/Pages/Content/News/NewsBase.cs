using QLN.Web.Shared.Components.ViewToggleButtons;
using QLN.Web.Shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using QLN.Common.Infrastructure.DTO_s;
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
        protected string _selectedView = "news";
        protected string selectedTabView = "News";
        protected string[] Tabs = new[] { "Qatar", "Middle East", "World", "Health & Education", "Community", "Law" };
        protected string SelectedTab = "Qatar";
        protected List<BannerItem> DailyHeroBanners { get; set; } = new();
        [Inject]
        protected NavigationManager navManager { get; set; }
        [Inject] private ILogger<NewsCardBase> Logger { get; set; }
        [Inject] private INewsService _newsService { get; set; }
        [Inject] private IEventService _eventService { get; set; }
        protected ContentPost topNewsSlot { get; set; } = new ContentPost();
        protected List<ContentPost> topNewsListSlot { get; set; } = new List<ContentPost>();
        protected List<ContentPost> moreArticleListSlot { get; set; } = new List<ContentPost>();
        protected List<ContentVideo> mostWatchedArticleListSlot { get; set; } = new List<ContentVideo>();
        protected List<ContentPost> articleListSlot1 { get; set; } = new List<ContentPost>(); // making this an event slot for now
        protected List<ContentPost> popularArticleListSlot { get; set; } = new List<ContentPost>();
        protected List<ContentPost> articleListSlot2 { get; set; } = new List<ContentPost>(); // making this an event slot for now
        protected QlnNewsNewsQatarPageResponse QatarNewsContent { get; set; } = new QlnNewsNewsQatarPageResponse();
        protected QlnNewsFinanceEntrepreneurshipPageResponse FinanceEntrepreneurshipNewsContent { get; set; } = new QlnNewsFinanceEntrepreneurshipPageResponse();
        protected QlnNewsFinanceRealEstatePageResponse RealEstateNewsContent { get; set; } = new QlnNewsFinanceRealEstatePageResponse();
        protected QlnNewsFinanceQatarPageResponse FinanceQatarNewsContent { get; set; } = new QlnNewsFinanceQatarPageResponse();
        protected QlnNewsFinanceMarketUpdatePageResponse MarketUpdateNewsContent { get; set; } = new QlnNewsFinanceMarketUpdatePageResponse();
        protected QlnNewsFinanceJobsCareersPageResponse JobCareersNewsContent { get; set; } = new QlnNewsFinanceJobsCareersPageResponse();
        protected QlnNewsFinanceFinancePageResponse FinanceFinanceNewsContent { get; set; } = new QlnNewsFinanceFinancePageResponse();
        protected QlnNewsNewsCommunityPageResponse CommunityNewsContent { get; set; } = new QlnNewsNewsCommunityPageResponse();
        protected QlnNewsNewsHealthEducationPageResponse HealthEducationNewsContent { get; set; } = new QlnNewsNewsHealthEducationPageResponse();
        protected QlnNewsNewsLawPageResponse LawsNewsContent { get; set; } = new QlnNewsNewsLawPageResponse();
        protected QlnNewsNewsMiddleEastPageResponse MiddleEastNewsContent { get; set; } = new QlnNewsNewsMiddleEastPageResponse();
        protected QlnNewsNewsWorldPageResponse WorldNewsContent { get; set; } = new QlnNewsNewsWorldPageResponse();
        protected QlnNewsLifestyleFoodDiningPageResponse FoodDiningNewsContent { get; set; } = new QlnNewsLifestyleFoodDiningPageResponse();
        protected QlnNewsLifestyleTravelLeisurePageResponse TravelLeisureNewsContent { get; set; } = new QlnNewsLifestyleTravelLeisurePageResponse();
        protected QlnNewsLifestyleArtsCulturePageResponse ArtsCultureNewsContent { get; set; } = new QlnNewsLifestyleArtsCulturePageResponse();
        protected QlnNewsLifestyleEventsPageResponse EventsNewsContent { get; set; } = new QlnNewsLifestyleEventsPageResponse();
        protected QlnNewsLifestyleFashionStylePageResponse FashionNewsContent { get; set; } = new QlnNewsLifestyleFashionStylePageResponse();
        protected QlnNewsLifestyleHomeLivingPageResponse HomeLivingNewsContent { get; set; } = new QlnNewsLifestyleHomeLivingPageResponse();
        protected QlnNewsSportsQatarSportsPageResponse QatarSportsNewsContent { get; set; } = new QlnNewsSportsQatarSportsPageResponse();
        protected QlnNewsSportsFootballPageResponse FoodBallNewsContent { get; set; } = new QlnNewsSportsFootballPageResponse();
        protected QlnNewsSportsInternationalPageResponse InternationalNewsContent { get; set; } = new QlnNewsSportsInternationalPageResponse();
        protected QlnNewsSportsMotorsportsPageResponse MotorSportsNewsContent { get; set; } = new QlnNewsSportsMotorsportsPageResponse();
        protected QlnNewsSportsOlympicsPageResponse OlympicsNewsContent { get; set; } = new QlnNewsSportsOlympicsPageResponse();
        protected QlnNewsSportsAthleteFeaturesPageResponse AthleteNewsContent { get; set; } = new QlnNewsSportsAthleteFeaturesPageResponse();
        protected List<BannerItem> NewsSideBanners { get; set; } = new();

        protected List<ContentVideo> VideoList { get; set; } = [];

        protected async override Task OnInitializedAsync()
        {
            try
            {
                PreSelectCategory();
                var bannersTask = LoadBanners(SelectedTab);
                await Task.WhenAll(bannersTask);
                QatarNewsContent = await GetNewsQatarAsync();

                // Top News Slot
                var topStoryItems = QatarNewsContent?.QlnNewsNewsQatar?.TopStory?.Items;
                if (topStoryItems != null && topStoryItems.Any())
                    topNewsSlot = topStoryItems.First();

                // Top News Stories Slot
                // this doesnt appear in the dataset as yet

                if (QatarNewsContent?.QlnNewsNewsQatar?.TopStory?.Items != null) // replace
                    topNewsListSlot = QatarNewsContent.QlnNewsNewsQatar.TopStory.Items; // replace

                // More Articles Slot

                if (QatarNewsContent?.QlnNewsNewsQatar?.MoreArticles?.Items != null)
                    moreArticleListSlot = QatarNewsContent.QlnNewsNewsQatar.MoreArticles.Items;

                // Most Watched Slot

                if (QatarNewsContent?.QlnNewsNewsQatar?.MostPopularArticles?.Items != null) // replace
                    mostWatchedArticleListSlot = new List<ContentVideo>(); // QatarNewsContent.QlnNewsNewsQatar.MostPopularArticles.Items; //replace

                // Articles 1 Slot

                if (QatarNewsContent?.QlnNewsNewsQatar?.Articles1?.Items != null)
                    articleListSlot1 = QatarNewsContent.QlnNewsNewsQatar.Articles1.Items;

                // Most Popular Articles Slot

                if (QatarNewsContent?.QlnNewsNewsQatar?.MostPopularArticles?.Items != null)
                    popularArticleListSlot = QatarNewsContent.QlnNewsNewsQatar.MostPopularArticles.Items;

                // Articles 2 Slot

                if (QatarNewsContent?.QlnNewsNewsQatar?.Articles2?.Items != null)
                    articleListSlot2 = QatarNewsContent.QlnNewsNewsQatar.Articles2.Items;

                var videoContent = await GetContentVideoLandingAsync();
                VideoList = videoContent?.QlnVideos?.QlnVideosTopVideos?.Items ?? [];

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
            selectedTabView = _viewOptions.FirstOrDefault(x => x.Value == _selectedView)?.Label;
            if (selectedCategory != null)
            {
                Tabs = selectedCategory.SubCategories.ToArray();
                SelectTab(Tabs.First());
            }
            else
            {
                Tabs = Array.Empty<string>();
            }
        }
        protected void PreSelectCategory()
        {
            var uri = navManager.ToAbsoluteUri(navManager.Uri);
            var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);
            string? localCategory = null;
            string? localSubcategory = null;
            Console.WriteLine("the method is called");
            if (query.TryGetValue("category", out var cat))
                localCategory = cat;

            if (query.TryGetValue("subcategory", out var sub))
                localSubcategory = sub;
            if (localCategory == "business")
            {
                SetViewMode("business");
                StateHasChanged();
            }
            else if (localCategory == "sports")
            {
                SetViewMode("sports");
            }
            else if (localCategory == "lifestyle")
            {
                SetViewMode("lifestyle");
                if (localSubcategory == "food-dining")
                {
                    Console.WriteLine("the subcategory is selected" + localSubcategory);
                    SelectTab("Food & Dining");
                }
                if (localSubcategory == "travel-leisure")
                {
                    SelectTab("Travel & Leisure");
                }
            }
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
            StateHasChanged();
            try
            {
                var banners = await FetchBannerData();
                NewsSideBanners = banners?.ContentNewsSide ?? new List<BannerItem>();
                DailyHeroBanners = banners?.ContentNewsHero ?? new List<BannerItem>();
                DailyTakeOverBanners = banners?.ContentNewsTakeover ?? new List<BannerItem>();
            }
            finally
            {
                isLoadingBanners = false;
            }
        }
        protected async Task<BannerResponse?> FetchBannerData()
        {
            try
            {
                var result = await _eventService.GetBannerAsync();
                if (result.IsSuccessStatusCode && result.Content != null)
                {
                    return await result.Content.ReadFromJsonAsync<BannerResponse>();
                }
                return null;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "FetchBannerData error.");
                return null;
            }
        }
        protected async void SelectTab(string tab)
        {
            isLoading = true;
            SelectedTab = tab;
            var bannersTask = LoadBanners(SelectedTab);
            // topNews = new ContentPost();
            // moreArticleList.Clear();
            switch (tab)
            {
                case "Qatar":
                    QatarNewsContent = await GetNewsAsync<QlnNewsNewsQatarPageResponse>("Qatar");
                    var qatar = QatarNewsContent?.QlnNewsNewsQatar;
                    topNewsSlot = qatar?.TopStory?.Items?.FirstOrDefault();
                    topNewsListSlot = qatar?.TopStory?.Items ?? new List<ContentPost>();
                    moreArticleListSlot = qatar?.MoreArticles?.Items ?? new List<ContentPost>();
                    mostWatchedArticleListSlot = new List<ContentVideo>();
                    articleListSlot1 = qatar?.Articles1?.Items ?? new List<ContentPost>();
                    popularArticleListSlot = qatar?.MostPopularArticles?.Items ?? new List<ContentPost>();
                    articleListSlot2 = qatar?.Articles2?.Items ?? new List<ContentPost>();
                    break;

                case "Community":
                    CommunityNewsContent = await GetNewsAsync<QlnNewsNewsCommunityPageResponse>("Community");
                    var community = CommunityNewsContent?.QlnNewsNewsCommunity;
                    topNewsSlot = community?.TopStory?.Items?.FirstOrDefault();
                    topNewsListSlot = community?.TopStory?.Items ?? new List<ContentPost>();
                    moreArticleListSlot = community?.MoreArticles?.Items ?? new List<ContentPost>();
                    mostWatchedArticleListSlot = new List<ContentVideo>();
                    articleListSlot1 = community?.Articles1?.Items ?? new List<ContentPost>();
                    popularArticleListSlot = community?.MostPopularArticles?.Items ?? new List<ContentPost>();
                    articleListSlot2 = community?.Articles2?.Items ?? new List<ContentPost>();
                    break;

                case "Law":
                    LawsNewsContent = await GetNewsAsync<QlnNewsNewsLawPageResponse>("Law");
                    var law = LawsNewsContent?.QlnNewsNewsLaw;
                    topNewsSlot = law?.TopStory?.Items?.FirstOrDefault();
                    topNewsListSlot = law?.TopStory?.Items ?? new List<ContentPost>();
                    moreArticleListSlot = law?.MoreArticles.Items ?? new List<ContentPost>();
                    mostWatchedArticleListSlot = new List<ContentVideo>();
                    articleListSlot1 = law?.Articles1?.Items ?? new List<ContentPost>();
                    popularArticleListSlot = law?.MostPopularArticles?.Items ?? new List<ContentPost>();
                    articleListSlot2 = law?.Articles2?.Items ?? new List<ContentPost>();
                    break;

                case "Health & Education":
                    HealthEducationNewsContent = await GetNewsAsync<QlnNewsNewsHealthEducationPageResponse>("Health & Education");
                    var health = HealthEducationNewsContent?.QlnNewsNewsHealthEducation;
                    topNewsSlot = health?.TopStory?.Items?.FirstOrDefault();
                    topNewsListSlot = health?.TopStory?.Items ?? new List<ContentPost>();
                    moreArticleListSlot = health?.MoreArticles?.Items ?? new List<ContentPost>();
                    mostWatchedArticleListSlot = new List<ContentVideo>();
                    articleListSlot1 = health?.Articles1?.Items ?? new List<ContentPost>();
                    popularArticleListSlot = health?.MostPopularArticles?.Items ?? new List<ContentPost>();
                    articleListSlot2 = health?.Articles2?.Items ?? new List<ContentPost>();
                    break;

                case "Middle East":
                    MiddleEastNewsContent = await GetNewsAsync<QlnNewsNewsMiddleEastPageResponse>("Middle East");
                    var middleEast = MiddleEastNewsContent?.QlnNewsNewsMiddleEast;
                    topNewsSlot = middleEast?.TopStory?.Items?.FirstOrDefault();
                    topNewsListSlot = middleEast?.TopStory?.Items ?? new List<ContentPost>();
                    moreArticleListSlot = middleEast?.MoreArticles?.Items ?? new List<ContentPost>();
                    mostWatchedArticleListSlot = new List<ContentVideo>();
                    articleListSlot1 = middleEast?.Articles1?.Items ?? new List<ContentPost>();
                    popularArticleListSlot = middleEast?.MostPopularArticles?.Items ?? new List<ContentPost>();
                    articleListSlot2 = middleEast?.Articles2?.Items ?? new List<ContentPost>();
                    break;

                case "Qatar Economy":
                    FinanceQatarNewsContent = await GetNewsAsync<QlnNewsFinanceQatarPageResponse>("Qatar Economy");
                    var economy = FinanceQatarNewsContent?.QlnNewsFinanceQatar;
                    topNewsSlot = economy?.TopStory?.Items?.FirstOrDefault();
                    topNewsListSlot = economy?.TopStory?.Items ?? new List<ContentPost>();
                    moreArticleListSlot = economy?.MoreArticles?.Items ?? new List<ContentPost>();
                    mostWatchedArticleListSlot = new List<ContentVideo>();
                    articleListSlot1 = economy?.Articles1?.Items ?? new List<ContentPost>();
                    popularArticleListSlot = economy?.MostPopularArticles?.Items ?? new List<ContentPost>();
                    articleListSlot2 = economy?.Articles2?.Items ?? new List<ContentPost>();
                    break;

                case "World":
                    WorldNewsContent = await GetNewsAsync<QlnNewsNewsWorldPageResponse>("World");
                    var world = WorldNewsContent?.QlnNewsNewsWorld;
                    topNewsSlot = world?.TopStory?.Items?.FirstOrDefault();
                    topNewsListSlot = world?.TopStory?.Items ?? new List<ContentPost>();
                    moreArticleListSlot = world?.MoreArticles?.Items ?? new List<ContentPost>();
                    mostWatchedArticleListSlot = new List<ContentVideo>();
                    articleListSlot1 = world?.Articles1?.Items ?? new List<ContentPost>();
                    popularArticleListSlot = world?.MostPopularArticles?.Items ?? new List<ContentPost>();
                    articleListSlot2 = world?.Articles2?.Items ?? new List<ContentPost>();
                    break;
                case "Market Updates":
                    MarketUpdateNewsContent = await GetNewsAsync<QlnNewsFinanceMarketUpdatePageResponse>("Market Updates");
                    var market = MarketUpdateNewsContent?.QlnNewsFinanceMarketUpdate;
                    topNewsSlot = market?.TopStory?.Items?.FirstOrDefault();
                    topNewsListSlot = market?.TopStory?.Items ?? new List<ContentPost>();
                    moreArticleListSlot = market?.MoreArticles?.Items ?? new List<ContentPost>();
                    mostWatchedArticleListSlot = new List<ContentVideo>();
                    articleListSlot1 = market?.Articles1?.Items ?? new List<ContentPost>();
                    popularArticleListSlot = market?.MostPopularArticles?.Items ?? new List<ContentPost>();
                    articleListSlot2 = market?.Articles2?.Items ?? new List<ContentPost>();
                    break;
                case "Real Estate":
                    RealEstateNewsContent = await GetNewsAsync<QlnNewsFinanceRealEstatePageResponse>("Real Estate");
                    var realEstate = RealEstateNewsContent?.QlnNewsFinanceRealEstate;
                    topNewsSlot = realEstate?.TopStory?.Items?.FirstOrDefault();
                    topNewsListSlot = realEstate?.TopStory?.Items ?? new List<ContentPost>();
                    moreArticleListSlot = realEstate?.MoreArticles?.Items ?? new List<ContentPost>();
                    mostWatchedArticleListSlot = new List<ContentVideo>();
                    articleListSlot1 = realEstate?.Articles1?.Items ?? new List<ContentPost>();
                    popularArticleListSlot = realEstate?.MostPopularArticles?.Items ?? new List<ContentPost>();
                    articleListSlot2 = realEstate?.Articles2?.Items ?? new List<ContentPost>();
                    break;
                case "Entrepreneurship":
                    FinanceEntrepreneurshipNewsContent = await GetNewsAsync<QlnNewsFinanceEntrepreneurshipPageResponse>("Entrepreneurship");
                    var entrepreneurship = FinanceEntrepreneurshipNewsContent?.QlnNewsFinanceEntrepreneurship;
                    topNewsSlot = entrepreneurship?.TopStory?.Items?.FirstOrDefault();
                    topNewsListSlot = entrepreneurship?.TopStory?.Items ?? new List<ContentPost>();
                    moreArticleListSlot = entrepreneurship?.MoreArticles?.Items ?? new List<ContentPost>();
                    mostWatchedArticleListSlot = new List<ContentVideo>();
                    articleListSlot1 = entrepreneurship?.Articles1?.Items ?? new List<ContentPost>();
                    popularArticleListSlot = entrepreneurship?.MostPopularArticles?.Items ?? new List<ContentPost>();
                    articleListSlot2 = entrepreneurship?.Articles2?.Items ?? new List<ContentPost>();
                    break;
                case "Finance":
                    FinanceFinanceNewsContent = await GetNewsAsync<QlnNewsFinanceFinancePageResponse>("Finance");
                    var finance = FinanceFinanceNewsContent?.QlnNewsFinanceFinance;
                    topNewsSlot = finance?.TopStory?.Items?.FirstOrDefault();
                    topNewsListSlot = finance?.TopStory?.Items ?? new List<ContentPost>();
                    moreArticleListSlot = finance?.MoreArticles?.Items ?? new List<ContentPost>();
                    mostWatchedArticleListSlot = new List<ContentVideo>();
                    articleListSlot1 = finance?.Articles1?.Items ?? new List<ContentPost>();
                    popularArticleListSlot = finance?.MostPopularArticles?.Items ?? new List<ContentPost>();
                    articleListSlot2 = finance?.Articles2?.Items ?? new List<ContentPost>();
                    break;
                case "Jobs & Careers":
                    JobCareersNewsContent = await GetNewsAsync<QlnNewsFinanceJobsCareersPageResponse>("Jobs & Careers");
                    var jobs = JobCareersNewsContent?.QlnNewsFinanceJobsCareers;
                    topNewsSlot = jobs?.TopStory?.Items?.FirstOrDefault();
                    topNewsListSlot = jobs?.TopStory?.Items ?? new List<ContentPost>();
                    moreArticleListSlot = jobs?.MoreArticles?.Items ?? new List<ContentPost>();
                    mostWatchedArticleListSlot = new List<ContentVideo>();
                    articleListSlot1 = jobs?.Articles1?.Items ?? new List<ContentPost>();
                    popularArticleListSlot = jobs?.MostPopularArticles?.Items ?? new List<ContentPost>();
                    articleListSlot2 = jobs?.Articles2?.Items ?? new List<ContentPost>();
                    break;
                case "Food & Dining":
                    FoodDiningNewsContent = await GetNewsAsync<QlnNewsLifestyleFoodDiningPageResponse>("Food & Dining");
                    var foods = FoodDiningNewsContent?.QlNewsLifestyleFoodDining;
                    topNewsSlot = foods?.TopStory?.Items?.FirstOrDefault();
                    topNewsListSlot = foods?.TopStory?.Items ?? new List<ContentPost>();
                    moreArticleListSlot = foods?.MoreArticles?.Items ?? new List<ContentPost>();
                    mostWatchedArticleListSlot = new List<ContentVideo>();
                    articleListSlot1 = foods?.Articles1?.Items ?? new List<ContentPost>();
                    popularArticleListSlot = foods?.MostPopularArticles?.Items ?? new List<ContentPost>();
                    articleListSlot2 = foods?.Articles2?.Items ?? new List<ContentPost>();
                    break;
                case "Travel & Leisure":
                    TravelLeisureNewsContent = await GetNewsAsync<QlnNewsLifestyleTravelLeisurePageResponse>("Travel & Leisure");
                    var travel = TravelLeisureNewsContent?.QlnNewsLifestyleTravelLeisure;
                    topNewsSlot = travel?.TopStory?.Items?.FirstOrDefault();
                    topNewsListSlot = travel?.TopStory?.Items ?? new List<ContentPost>();
                    moreArticleListSlot = travel?.MoreArticles?.Items ?? new List<ContentPost>();
                    mostWatchedArticleListSlot = new List<ContentVideo>();
                    articleListSlot1 = travel?.Articles1?.Items ?? new List<ContentPost>();
                    popularArticleListSlot = travel?.MostPopularArticles?.Items ?? new List<ContentPost>();
                    articleListSlot2 = travel?.Articles2?.Items ?? new List<ContentPost>();
                    break;
                case "Arts & Culture":
                    ArtsCultureNewsContent = await GetNewsAsync<QlnNewsLifestyleArtsCulturePageResponse>("Arts & Culture");
                    var arts = ArtsCultureNewsContent?.QlnNewsLifestyleArtsCulture;
                    topNewsSlot = arts?.TopStory?.Items?.FirstOrDefault();
                    topNewsListSlot = arts?.TopStory?.Items ?? new List<ContentPost>();
                    moreArticleListSlot = arts?.MoreArticles?.Items ?? new List<ContentPost>();
                    mostWatchedArticleListSlot = new List<ContentVideo>();
                    articleListSlot1 = arts?.Articles1?.Items ?? new List<ContentPost>();
                    popularArticleListSlot = arts?.MostPopularArticles?.Items ?? new List<ContentPost>();
                    articleListSlot2 = arts?.Articles2?.Items ?? new List<ContentPost>();
                    break;
                case "Events":
                    EventsNewsContent = await GetNewsAsync<QlnNewsLifestyleEventsPageResponse>("Events");
                    var events = EventsNewsContent?.QlnNewsLifestyleEvents;
                    topNewsSlot = events?.TopStory?.Items?.FirstOrDefault();
                    topNewsListSlot = events?.MoreArticles?.Items ?? new List<ContentPost>();
                    moreArticleListSlot = events?.MoreArticles?.Items ?? new List<ContentPost>();
                    mostWatchedArticleListSlot = new List<ContentVideo>();
                    articleListSlot1 = events?.Articles1?.Items ?? new List<ContentPost>();
                    popularArticleListSlot = events?.MostPopularArticles?.Items ?? new List<ContentPost>();
                    articleListSlot2 = events?.Articles2?.Items ?? new List<ContentPost>();
                    break;
                case "Fashion & Style":
                    FashionNewsContent = await GetNewsAsync<QlnNewsLifestyleFashionStylePageResponse>("Fashion & Style");
                    var fashion = FashionNewsContent?.QlnNewsLifestyleFashionStyle;
                    topNewsSlot = fashion?.TopStory?.Items?.FirstOrDefault();
                    topNewsListSlot = fashion?.TopStory?.Items ?? new List<ContentPost>();
                    moreArticleListSlot = fashion?.MoreArticles?.Items ?? new List<ContentPost>();
                    mostWatchedArticleListSlot = new List<ContentVideo>();
                    articleListSlot1 = fashion?.Articles1?.Items ?? new List<ContentPost>();
                    popularArticleListSlot = fashion?.MostPopularArticles?.Items ?? new List<ContentPost>();
                    articleListSlot2 = fashion?.Articles2?.Items ?? new List<ContentPost>();
                    break;
                case "Home & Living":
                    HomeLivingNewsContent = await GetNewsAsync<QlnNewsLifestyleHomeLivingPageResponse>("Home & Living");
                    var home = HomeLivingNewsContent?.QlnNewsLifestyleHomeLiving;
                    topNewsSlot = home?.TopStory?.Items?.FirstOrDefault();
                    topNewsListSlot = home?.TopStory?.Items ?? new List<ContentPost>();
                    moreArticleListSlot = home?.MoreArticles?.Items ?? new List<ContentPost>();
                    mostWatchedArticleListSlot = new List<ContentVideo>();
                    articleListSlot1 = home?.Articles1?.Items ?? new List<ContentPost>();
                    popularArticleListSlot = home?.MostPopularArticles?.Items ?? new List<ContentPost>();
                    articleListSlot2 = home?.Articles2?.Items ?? new List<ContentPost>();
                    break;
                case "Qatar Sports":
                    QatarSportsNewsContent = await GetNewsAsync<QlnNewsSportsQatarSportsPageResponse>("Qatar Sports");
                    var sports = QatarSportsNewsContent?.QlnNewsSportsQatarSports;
                    topNewsSlot = sports?.TopStory?.Items?.FirstOrDefault();
                    topNewsListSlot = sports?.TopStory?.Items ?? new List<ContentPost>();
                    moreArticleListSlot = sports?.MoreArticles?.Items ?? new List<ContentPost>();
                    mostWatchedArticleListSlot = new List<ContentVideo>();
                    articleListSlot1 = sports?.Articles1?.Items ?? new List<ContentPost>();
                    popularArticleListSlot = sports?.MostPopularArticles?.Items ?? new List<ContentPost>();
                    articleListSlot2 = sports?.Articles2?.Items ?? new List<ContentPost>();
                    break;
                case "Football":
                    FoodBallNewsContent = await GetNewsAsync<QlnNewsSportsFootballPageResponse>("Football");
                    var football = FoodBallNewsContent?.QlnNewsSportsFootball;
                    topNewsSlot = football?.TopStory?.Items?.FirstOrDefault();
                    topNewsListSlot = football?.TopStory?.Items ?? new List<ContentPost>();
                    moreArticleListSlot = football?.MoreArticles?.Items ?? new List<ContentPost>();
                    mostWatchedArticleListSlot = new List<ContentVideo>();
                    articleListSlot1 = football?.Articles1?.Items ?? new List<ContentPost>();
                    popularArticleListSlot = football?.MostPopularArticles?.Items ?? new List<ContentPost>();
                    articleListSlot2 = football?.Articles2?.Items ?? new List<ContentPost>();
                    break;
                case "International":
                    InternationalNewsContent = await GetNewsAsync<QlnNewsSportsInternationalPageResponse>("International");
                    var international = InternationalNewsContent?.QlnNewsSportsInternational;
                    topNewsSlot = international?.TopStory?.Items?.FirstOrDefault();
                    topNewsListSlot = international?.TopStory?.Items ?? new List<ContentPost>();
                    moreArticleListSlot = international?.MoreArticles?.Items ?? new List<ContentPost>();
                    mostWatchedArticleListSlot = new List<ContentVideo>();
                    articleListSlot1 = international?.Articles1?.Items ?? new List<ContentPost>();
                    popularArticleListSlot = international?.MostPopularArticles?.Items ?? new List<ContentPost>();
                    articleListSlot2 = international?.Articles2?.Items ?? new List<ContentPost>();
                    break;
                case "Motorsports":
                    MotorSportsNewsContent = await GetNewsAsync<QlnNewsSportsMotorsportsPageResponse>("Motorsports");
                    var motorSports = MotorSportsNewsContent?.QlnNewsSportsMotorsports;
                    topNewsSlot = motorSports?.TopStory?.Items?.FirstOrDefault();
                    topNewsListSlot = motorSports?.TopStory?.Items ?? new List<ContentPost>();
                    moreArticleListSlot = motorSports?.MoreArticles?.Items ?? new List<ContentPost>();
                    mostWatchedArticleListSlot = new List<ContentVideo>();
                    articleListSlot1 = motorSports?.Articles1?.Items ?? new List<ContentPost>();
                    popularArticleListSlot = motorSports?.MostPopularArticles?.Items ?? new List<ContentPost>();
                    articleListSlot2 = motorSports?.Articles2?.Items ?? new List<ContentPost>();
                    break;
                case "Olympics":
                    OlympicsNewsContent = await GetNewsAsync<QlnNewsSportsOlympicsPageResponse>("Olympics");
                    var olympics = OlympicsNewsContent?.QlnNewsSportsOlympics;
                    topNewsSlot = olympics?.TopStory?.Items?.FirstOrDefault();
                    topNewsListSlot = olympics?.TopStory?.Items ?? new List<ContentPost>();
                    moreArticleListSlot = olympics?.MoreArticles?.Items ?? new List<ContentPost>();
                    mostWatchedArticleListSlot = new List<ContentVideo>();
                    articleListSlot1 = olympics?.Articles1?.Items ?? new List<ContentPost>();
                    popularArticleListSlot = olympics?.MostPopularArticles?.Items ?? new List<ContentPost>();
                    articleListSlot2 = olympics?.Articles2?.Items ?? new List<ContentPost>();
                    break;
                case "Athlete Features":
                    AthleteNewsContent = await GetNewsAsync<QlnNewsSportsAthleteFeaturesPageResponse>("Athlete Features");
                    var athelete = AthleteNewsContent?.QlnNewsSportsAthleteFeatures;
                    topNewsSlot = athelete?.TopStory?.Items?.FirstOrDefault();
                    topNewsListSlot = athelete?.TopStory?.Items ?? new List<ContentPost>();
                    moreArticleListSlot = athelete?.MoreArticles?.Items ?? new List<ContentPost>();
                    mostWatchedArticleListSlot = new List<ContentVideo>();
                    articleListSlot1 = athelete?.Articles1?.Items ?? new List<ContentPost>();
                    popularArticleListSlot = athelete?.MostPopularArticles?.Items ?? new List<ContentPost>();
                    articleListSlot2 = athelete?.Articles2?.Items ?? new List<ContentPost>();
                    break;
            }
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
        protected async Task<QlnNewsNewsQatarPageResponse> GetNewsQatarAsync()
        {
            try
            {
                var apiResponse = await _newsService.GetNewsQatarAsync() ?? new HttpResponseMessage();
                if (apiResponse.IsSuccessStatusCode && apiResponse.Content != null)
                {
                    var response = await apiResponse.Content.ReadFromJsonAsync<QlnNewsNewsQatarPageResponse>();
                    return response ?? new QlnNewsNewsQatarPageResponse();
                }
                return new QlnNewsNewsQatarPageResponse();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetNewsQatarAsync");
                return new QlnNewsNewsQatarPageResponse();
            }
        }
        protected async Task<QlnNewsNewsCommunityPageResponse> GetNewsCommunityAsync()
        {
            try
            {
                var apiResponse = await _newsService.GetNewsCommunityAsync() ?? new HttpResponseMessage();

                if (apiResponse.IsSuccessStatusCode && apiResponse.Content != null)
                {
                    var response = await apiResponse.Content.ReadFromJsonAsync<QlnNewsNewsCommunityPageResponse>();
                    return response ?? new QlnNewsNewsCommunityPageResponse();
                }
                return new QlnNewsNewsCommunityPageResponse();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetNewsCommunityAsync");
                return new QlnNewsNewsCommunityPageResponse();
            }
        }
        protected async Task<QlnNewsNewsHealthEducationPageResponse> GetNewsHealthAndEducationAsync()
        {
            try
            {
                var apiResponse = await _newsService.GetNewsHealthAndEducationAsync() ?? new HttpResponseMessage();

                if (apiResponse.IsSuccessStatusCode && apiResponse.Content != null)
                {
                    var response = await apiResponse.Content.ReadFromJsonAsync<QlnNewsNewsHealthEducationPageResponse>();
                    return response ?? new QlnNewsNewsHealthEducationPageResponse();
                }
                return new QlnNewsNewsHealthEducationPageResponse();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetNewsHealthAndEducationAsync");
                return new QlnNewsNewsHealthEducationPageResponse();
            }
        }
        protected async Task<QlnNewsNewsLawPageResponse> GetNewsLawAsync()
        {
            try
            {
                var apiResponse = await _newsService.GetNewsLawAsync() ?? new HttpResponseMessage();

                if (apiResponse.IsSuccessStatusCode && apiResponse.Content != null)
                {
                    var response = await apiResponse.Content.ReadFromJsonAsync<QlnNewsNewsLawPageResponse>();
                    return response ?? new QlnNewsNewsLawPageResponse();
                }
                return new QlnNewsNewsLawPageResponse();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GGetNewsLawAsync");
                return new QlnNewsNewsLawPageResponse();
            }
        }
        protected async Task<QlnNewsNewsMiddleEastPageResponse> GetNewsMiddleEastAsync()
        {
            try
            {
                var apiResponse = await _newsService.GetNewsMiddleEastAsync() ?? new HttpResponseMessage();
                if (apiResponse.IsSuccessStatusCode && apiResponse.Content != null)
                {
                    var response = await apiResponse.Content.ReadFromJsonAsync<QlnNewsNewsMiddleEastPageResponse>();
                    return response ?? new QlnNewsNewsMiddleEastPageResponse();
                }
                return new QlnNewsNewsMiddleEastPageResponse();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetNewsMiddleEastAsync");
                return new QlnNewsNewsMiddleEastPageResponse();
            }
        }
        protected async Task<QlnNewsNewsWorldPageResponse> GetNewsWorldAsync()
        {
            try
            {
                var apiResponse = await _newsService.GetNewsWorldAsync() ?? new HttpResponseMessage();

                if (apiResponse.IsSuccessStatusCode && apiResponse.Content != null)
                {
                    var response = await apiResponse.Content.ReadFromJsonAsync<QlnNewsNewsWorldPageResponse>();
                    return response ?? new QlnNewsNewsWorldPageResponse();
                }
                return new QlnNewsNewsWorldPageResponse();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetNewsWorldAsync");
                return new QlnNewsNewsWorldPageResponse();
            }
        }

        protected void onclick(ContentPost news)
        {
            selectedTabView = _viewOptions.FirstOrDefault(x => x.Value == _selectedView)?.Label;
            navManager.NavigateTo($"/content/article/details/{selectedTabView}/{news.Slug}");
        }


        /// <summary>
        /// Gets Content Videos Page data
        /// </summary>
        /// <returns>ContentsVideosResponse</returns>
        protected async Task<ContentsVideosResponse> GetContentVideoLandingAsync()
        {
            try
            {
                var apiResponse = await _contentService.GetVideosLPAsync() ?? new HttpResponseMessage();

                if (apiResponse.IsSuccessStatusCode && apiResponse.Content != null)
                {
                    var response = await apiResponse.Content.ReadFromJsonAsync<ContentsVideosResponse>();
                    return response ?? new ContentsVideosResponse();
                }

                return new ContentsVideosResponse();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message, "GetContentVideoLandingAsync");
                return new ContentsVideosResponse();
            }
        }
    }
}


