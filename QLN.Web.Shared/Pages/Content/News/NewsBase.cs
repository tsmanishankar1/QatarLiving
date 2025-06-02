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
        public bool isLoading = true;
        protected bool isLoadingBanners = true;
        protected List<BannerItem> DailyTakeOverBanners = new();
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
        protected List<BannerItem> DailyHeroBanners { get; set; } = new();
        [Inject]
        protected NavigationManager navManager { get; set; }
        [Inject] private ILogger<NewsCardBase> Logger { get; set; }
        [Inject] private INewsService _newsService { get; set; }
        [Inject] private IEventService _eventService { get; set; }
        protected ContentPost topNews { get; set; } = new ContentPost();
        protected List<ContentPost> moreArticleList { get; set; } = new List<ContentPost>();
        protected List<ContentPost> secondArticleList { get; set; } = new List<ContentPost>();
        protected List<ContentPost> popularArticleList { get; set; } = new List<ContentPost>();
        protected List<ContentPost> finalArticleList { get; set; } = new List<ContentPost>();
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
        // protected QlnNews QatarSportsNewsContent { get; set; } = new NewsQatarSportsPageResponse();










        protected async override Task OnInitializedAsync()
        {
            try
            {
                var bannersTask = LoadBanners();
                await Task.WhenAll(bannersTask);
                QatarNewsContent = await GetNewsQatarAsync();

                if (QatarNewsContent?.QlnNewsNewsQatar?.MoreArticles?.Items != null)
                    moreArticleList = QatarNewsContent.QlnNewsNewsQatar.MoreArticles.Items;
                if (QatarNewsContent?.QlnNewsNewsQatar?.Articles1?.Items != null)
                    secondArticleList = QatarNewsContent.QlnNewsNewsQatar.Articles1.Items;
                 if (QatarNewsContent?.QlnNewsNewsQatar?.Articles2?.Items != null)
                    finalArticleList = QatarNewsContent.QlnNewsNewsQatar.Articles2.Items;
                if (QatarNewsContent?.QlnNewsNewsQatar?.MostPopularArticles?.Items != null)
                    popularArticleList = QatarNewsContent.QlnNewsNewsQatar.MostPopularArticles.Items;
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
                SelectTab(Tabs.First());
            }
            else
            {
                Tabs = Array.Empty<string>();
            }
        }
        private async Task LoadBanners()
        {
            isLoadingBanners = true;
            try
            {
                var banners = await FetchBannerData();
                DailyHeroBanners = banners?.NewsQatarHero ?? new List<BannerItem>();
                DailyTakeOverBanners = banners?.NewsQatarTakeOver1 ?? new List<BannerItem>();
            }
            finally
            {
                isLoadingBanners = false;
            }
        }
        private async Task<BannerResponse?> FetchBannerData()
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
            topNews = new ContentPost();
            moreArticleList.Clear();
            switch (tab)
            {
                case "Qatar":
                    QatarNewsContent = await GetNewsAsync<QlnNewsNewsQatarPageResponse>("Qatar");
                    var qatar = QatarNewsContent?.QlnNewsNewsQatar;
                    topNews = qatar?.TopStory?.Items?.FirstOrDefault();
                    moreArticleList = qatar?.MoreArticles?.Items ?? new List<ContentPost>();
                    secondArticleList = qatar?.Articles1?.Items ?? new List<ContentPost>();
                    finalArticleList = qatar?.Articles2?.Items ?? new List<ContentPost>();
                    popularArticleList = qatar?.MostPopularArticles?.Items ?? new List<ContentPost>();
                    break;

                case "Community":
                    CommunityNewsContent = await GetNewsAsync<QlnNewsNewsCommunityPageResponse>("Community");
                    var community = CommunityNewsContent?.QlnNewsNewsCommunity;
                    topNews = community?.TopStory?.Items?.FirstOrDefault();
                    moreArticleList = community?.MoreArticles?.Items ?? new List<ContentPost>();
                     secondArticleList = community?.Articles1?.Items ?? new List<ContentPost>();
                    finalArticleList = community?.Articles2?.Items ?? new List<ContentPost>();
                    popularArticleList = community?.MostPopularArticles?.Items ?? new List<ContentPost>();
                    break;

                case "Law":
                    LawsNewsContent = await GetNewsAsync<QlnNewsNewsLawPageResponse>("Law");
                    var law = LawsNewsContent?.QlnNewsNewsLaw;
                    topNews = law?.TopStory?.Items?.FirstOrDefault();
                    moreArticleList = law?.MoreArticles.Items ?? new List<ContentPost>();
                    secondArticleList = law?.Articles1?.Items ?? new List<ContentPost>();
                    finalArticleList = law?.Articles2?.Items ?? new List<ContentPost>();
                    popularArticleList = law?.MostPopularArticles?.Items ?? new List<ContentPost>();
                    break;

                case "Health & Education":
                    HealthEducationNewsContent = await GetNewsAsync<QlnNewsNewsHealthEducationPageResponse>("Health & Education");
                    var health = HealthEducationNewsContent?.QlnNewsNewsHealthEducation;
                    topNews = health?.TopStory?.Items?.FirstOrDefault();
                    moreArticleList = health?.MoreArticles?.Items ?? new List<ContentPost>();
                    secondArticleList = health?.Articles1?.Items ?? new List<ContentPost>();
                    finalArticleList = health?.Articles2?.Items ?? new List<ContentPost>();
                    popularArticleList = health?.MostPopularArticles?.Items ?? new List<ContentPost>();
                    break;

                case "Middle East":
                    MiddleEastNewsContent = await GetNewsAsync<QlnNewsNewsMiddleEastPageResponse>("Middle East");
                    var middleEast = MiddleEastNewsContent?.QlnNewsNewsMiddleEast;
                    topNews = middleEast?.TopStory?.Items?.FirstOrDefault();
                    moreArticleList = middleEast?.MoreArticles?.Items ?? new List<ContentPost>();
                    secondArticleList = middleEast?.Articles1?.Items ?? new List<ContentPost>();
                    finalArticleList = middleEast?.Articles2?.Items ?? new List<ContentPost>();
                    popularArticleList = middleEast?.MostPopularArticles?.Items ?? new List<ContentPost>();
                    break;

                case "Qatar Economy":
                    FinanceQatarNewsContent = await GetNewsAsync<QlnNewsFinanceQatarPageResponse>("Qatar Economy");
                    var economy = FinanceQatarNewsContent?.QlnNewsFinanceQatar;
                    topNews = economy?.TopStory?.Items?.FirstOrDefault();
                    moreArticleList = economy?.MoreArticles?.Items ?? new List<ContentPost>();
                    secondArticleList = economy?.Articles1?.Items ?? new List<ContentPost>();
                    finalArticleList = economy?.Articles2?.Items ?? new List<ContentPost>();
                    popularArticleList = economy?.MostPopularArticles?.Items ?? new List<ContentPost>();
                    break;

                case "World":
                    WorldNewsContent = await GetNewsAsync<QlnNewsNewsWorldPageResponse>("World");
                    var world = WorldNewsContent?.QlnNewsNewsWorld;
                    topNews = world?.TopStory?.Items?.FirstOrDefault();
                    moreArticleList = world?.MoreArticles?.Items ?? new List<ContentPost>();
                    secondArticleList = world?.Articles1?.Items ?? new List<ContentPost>();
                    finalArticleList = world?.Articles2?.Items ?? new List<ContentPost>();
                    popularArticleList = world?.MostPopularArticles?.Items ?? new List<ContentPost>();
                    break;
                case "Market Updates":
                    MarketUpdateNewsContent = await GetNewsAsync<QlnNewsFinanceMarketUpdatePageResponse>("Market Updates");
                    var market = MarketUpdateNewsContent?.QlnNewsFinanceMarketUpdate;
                    topNews = market?.TopStory?.Items?.FirstOrDefault();
                    moreArticleList = market?.MoreArticles?.Items ?? new List<ContentPost>();
                    secondArticleList = market?.Articles1?.Items ?? new List<ContentPost>();
                    finalArticleList = market?.Articles2?.Items ?? new List<ContentPost>();
                    popularArticleList = market?.MostPopularArticles?.Items ?? new List<ContentPost>();
                    break;
                case "Real Estate":
                    RealEstateNewsContent = await GetNewsAsync<QlnNewsFinanceRealEstatePageResponse>("Real Estate");
                    var realEstate = RealEstateNewsContent?.QlnNewsFinanceRealEstate;
                    topNews = realEstate?.TopStory?.Items?.FirstOrDefault();
                    moreArticleList = realEstate?.MoreArticles?.Items ?? new List<ContentPost>();
                    secondArticleList = realEstate?.Articles1?.Items ?? new List<ContentPost>();
                    finalArticleList = realEstate?.Articles2?.Items ?? new List<ContentPost>();
                    popularArticleList = realEstate?.MostPopularArticles?.Items ?? new List<ContentPost>();
                    break;
                case "Entrepreneurship":
                    FinanceEntrepreneurshipNewsContent = await GetNewsAsync<QlnNewsFinanceEntrepreneurshipPageResponse>("Entrepreneurship");
                    var entrepreneurship = FinanceEntrepreneurshipNewsContent?.QlnNewsFinanceEntrepreneurship;
                    topNews = entrepreneurship?.TopStory?.Items?.FirstOrDefault();
                    moreArticleList = entrepreneurship?.MoreArticles?.Items ?? new List<ContentPost>();
                    secondArticleList = entrepreneurship?.Articles1?.Items ?? new List<ContentPost>();
                    finalArticleList = entrepreneurship?.Articles2?.Items ?? new List<ContentPost>();
                    popularArticleList = entrepreneurship?.MostPopularArticles?.Items ?? new List<ContentPost>();
                    break;
                case "Finance":
                    FinanceFinanceNewsContent = await GetNewsAsync<QlnNewsFinanceFinancePageResponse>("Finance");
                    var finance = FinanceFinanceNewsContent?.QlnNewsFinanceFinance;
                    topNews = finance?.TopStory?.Items?.FirstOrDefault();
                    moreArticleList = finance?.MoreArticles?.Items ?? new List<ContentPost>();
                     secondArticleList = finance?.Articles1?.Items ?? new List<ContentPost>();
                    finalArticleList = finance?.Articles2?.Items ?? new List<ContentPost>();
                    popularArticleList = finance?.MostPopularArticles?.Items ?? new List<ContentPost>();
                    break;
                case "Jobs & Careers":
                    JobCareersNewsContent = await GetNewsAsync<QlnNewsFinanceJobsCareersPageResponse>("Jobs & Careers");
                    var jobs = JobCareersNewsContent?.QlnNewsFinanceJobsCareers;
                    topNews = jobs?.TopStory?.Items?.FirstOrDefault();
                    moreArticleList = jobs?.MoreArticles?.Items ?? new List<ContentPost>();
                    secondArticleList = jobs?.Articles1?.Items ?? new List<ContentPost>();
                    finalArticleList = jobs?.Articles2?.Items ?? new List<ContentPost>();
                    popularArticleList = jobs?.MostPopularArticles?.Items ?? new List<ContentPost>();
                    break;
                case "Food & Dining":
                    FoodDiningNewsContent = await GetNewsAsync<QlnNewsLifestyleFoodDiningPageResponse>("Food & Dining");
                    var foods = FoodDiningNewsContent?.QlNewsLifestyleFoodDining;
                    topNews = foods?.TopStory?.Items?.FirstOrDefault();
                    moreArticleList = foods?.MoreArticles?.Items ?? new List<ContentPost>();
                    secondArticleList = foods?.Articles1?.Items ?? new List<ContentPost>();
                    finalArticleList = foods?.Articles2?.Items ?? new List<ContentPost>();
                    popularArticleList = foods?.MostPopularArticles?.Items ?? new List<ContentPost>();
                    break;
                case "Travel & Leisure":
                    TravelLeisureNewsContent = await GetNewsAsync<QlnNewsLifestyleTravelLeisurePageResponse>("Travel & Leisure");
                    var travel = TravelLeisureNewsContent?.QlnNewsLifestyleTravelLeisure;
                    topNews = travel?.TopStory?.Items?.FirstOrDefault();
                    moreArticleList = travel?.MoreArticles?.Items ?? new List<ContentPost>();
                    secondArticleList = travel?.Articles1?.Items ?? new List<ContentPost>();
                    finalArticleList = travel?.Articles2?.Items ?? new List<ContentPost>();
                    popularArticleList = travel?.MostPopularArticles?.Items ?? new List<ContentPost>();
                    break;
                case "Arts & Culture":
                    ArtsCultureNewsContent = await GetNewsAsync<QlnNewsLifestyleArtsCulturePageResponse>("Arts & Culture");
                    var arts = ArtsCultureNewsContent?.QlnNewsLifestyleArtsCulture;
                    topNews = arts?.TopStory?.Items?.FirstOrDefault();
                    moreArticleList = arts?.MoreArticles?.Items ?? new List<ContentPost>();
                    secondArticleList = arts?.Articles1?.Items ?? new List<ContentPost>();
                    finalArticleList = arts?.Articles2?.Items ?? new List<ContentPost>();
                    popularArticleList = arts?.MostPopularArticles?.Items ?? new List<ContentPost>();
                    break;
                case "Events":
                EventsNewsContent = await GetNewsAsync<QlnNewsLifestyleEventsPageResponse>("Events");
                var events = EventsNewsContent?.QlnNewsLifestyleEvents;
                topNews = events?.TopStory?.Items?.FirstOrDefault();
                moreArticleList = events?.MoreArticles?.Items ?? new List<ContentPost>();
                secondArticleList = events?.Articles1?.Items ?? new List<ContentPost>();
                finalArticleList = events?.Articles2?.Items ?? new List<ContentPost>();
                popularArticleList = events?.MostPopularArticles?.Items ?? new List<ContentPost>();
                break;
                case "Fashion & Style":
                    FashionNewsContent = await GetNewsAsync<QlnNewsLifestyleFashionStylePageResponse>("Fashion & Style");
                    var fashion = FashionNewsContent?.QlnNewsLifestyleFashionStyle;
                    topNews = fashion?.TopStory?.Items?.FirstOrDefault();
                    moreArticleList = fashion?.MoreArticles?.Items ?? new List<ContentPost>();
                    secondArticleList = fashion?.Articles1?.Items ?? new List<ContentPost>();
                    finalArticleList = fashion?.Articles2?.Items ?? new List<ContentPost>();
                    popularArticleList = fashion?.MostPopularArticles?.Items ?? new List<ContentPost>();
                    break;
                case "Home & Living":
                    HomeLivingNewsContent = await GetNewsAsync<QlnNewsLifestyleHomeLivingPageResponse>("Home & Living");
                    var home = HomeLivingNewsContent?.QlnNewsLifestyleHomeLiving;
                    topNews = home?.TopStory?.Items?.FirstOrDefault();
                    moreArticleList = home?.MoreArticles?.Items ?? new List<ContentPost>();
                    secondArticleList = home?.Articles1?.Items ?? new List<ContentPost>();
                    finalArticleList = home?.Articles2?.Items ?? new List<ContentPost>();
                    popularArticleList = home?.MostPopularArticles?.Items ?? new List<ContentPost>();
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
                Console.WriteLine("reached news apii");
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
            navManager.NavigateTo($"/content/article/details/{news.Slug}");
        }
    }
}


