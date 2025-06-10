
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using QLN.Web.Shared.Model;
using QLN.Web.Shared.Components.ViewToggleButtons;
using System.Globalization;
using QLN.Web.Shared.Models;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Web.Shared.Services.Interface;
using System.Net.Http.Json;

public class ArticleBase : ComponentBase
{
    [Parameter]
    public string slug { get; set; }
    public string category { get; set; }
    public string categoryLabel { get; set; }
    public string subcategory { get; set; }
    public string subcategoryLabel { get; set; }
    public bool isLoading { get; set; } = true;
    protected bool imageLoaded = false;
    protected List<BannerItem> DailyHeroBanners { get; set; } = new();
    protected List<BannerItem> ArticleSideBanners { get; set; } = new();
    protected bool isLoadingBanners = true;
    public List<QLN.Web.Shared.Components.BreadCrumb.BreadcrumbItem> breadcrumbItems = new();
    protected QLN.Web.Shared.Components.BreadCrumb.BreadcrumbItem? postBreadcrumbItem;
    protected QLN.Web.Shared.Components.BreadCrumb.BreadcrumbItem? postBreadcrumbCategory;
    protected QlnNewsNewsQatarPageResponse QatarNewsContent { get; set; } = new QlnNewsNewsQatarPageResponse();
    protected List<ContentPost> moreArticleList { get; set; } = new List<ContentPost>();
    protected GenericNewsPageResponse? NewsContent { get; set; }
    [Inject] private ILogger<NewsCardBase> Logger { get; set; }
    [Inject] private ISimpleMemoryCache _simpleCacheService { get; set; }
    public PostModel SelectedPost { get; set; } = new PostModel
    {
        Id = string.Empty,
        Category = string.Empty,
        Title = string.Empty,
        ImageUrl = null,
        BodyPreview = string.Empty,
        Author = string.Empty,
        slug = string.Empty,
        Time = DateTime.MinValue,
        LikeCount = 0,
        CommentCount = 0,
        isCommented = false,
        Slug = string.Empty,
        Comments = new List<CommentModel>()
    };
    [Inject] private INewsService _newsService { get; set; }
    [Inject] private IEventService _eventService { get; set; }
    protected ContentPost newsArticle { get; set; } = new ContentPost();
    protected int commentsCount = 0;
    public List<string> carouselImages = new()
    {
        "/images/banner_image.svg",
        "/images/banner_image.svg",
        "/images/banner_image.svg"
    };
    [Inject]
    protected NavigationManager navManager { get; set; }
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

    [Parameter]
    public NewsItem Item { get; set; }
    protected async override Task OnInitializedAsync()
    {
        try
        {
            var uri = navManager.ToAbsoluteUri(navManager.Uri);
            var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);
            if (query.TryGetValue("category", out var cat))
                category = cat;
            if (query.TryGetValue("subcategory", out var sub))
                subcategory = sub;
            categoryLabel = routerList.FirstOrDefault(item => item.Value == category)?.Label;
            subcategoryLabel = routerList.FirstOrDefault(item => item.Value == subcategory)?.Label;
            isLoading = true;
            var bannersTask = LoadBanners();
            await Task.WhenAll(bannersTask);
            isLoading = true;
            newsArticle = await GetNewsBySlugAsync();
            SelectedPost = new PostModel
            {
                Id = newsArticle.Nid,
                Category = newsArticle.Category,
                Title = newsArticle.Title,
                BodyPreview = newsArticle.Description,
                Author = "",
                Time = DateTime.TryParse(newsArticle.DateCreated, out var parsedDate) ? parsedDate : DateTime.MinValue,
                LikeCount = 0,
                CommentCount = newsArticle.Comments?.Count ?? 0,
                ImageUrl = newsArticle.ImageUrl,
                slug = newsArticle.Slug,
                isCommented = true,
                Comments = newsArticle.Comments?.Select(c => new CommentModel
                {
                    CreatedBy = c.Username,
                    CreatedAt = !string.IsNullOrWhiteSpace(c.CreatedDate) && DateTime.TryParseExact(
                    c.CreatedDate,
                    "yyyy-MM-dd HH:mm:ss",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var commentDate
                    ) ? commentDate : DateTime.MinValue,
                    Description = c.Subject,
                    LikeCount = 0,
                    UnlikeCount = 0,
                    Avatar = "/images/content/Sample.svg"
                }).ToList() ?? new List<CommentModel>()
            };
            if (category != null && subcategory != null)
            {
                breadcrumbItems = new()
                {
                new() {   Label = categoryLabel,Url =$"/content/news?category={category}" },
                new() { Label = subcategoryLabel, Url = $"/content/news?category={category}&subcategory={subcategory}"},
                new() { Label = newsArticle.Title, Url = $"/content/article/details/{category}/{subcategory}/{slug}", IsLast = true },
                };
            }
            else
            {
                breadcrumbItems = new()
                {
                new() { Label = "Daily", Url = "/content/daily"},
                new() { Label = SelectedPost.Category, Url = $"/content/news"},

                new() { Label = newsArticle.Title, Url = $"/content/article/details/{slug}", IsLast = true },
                };
            }

            // QatarNewsContent = await GetNewsQatarAsync();
            NewsContent = await GetNewsAsync<GenericNewsPageResponse>(subcategoryLabel);
            moreArticleList = NewsContent?.News?.MoreArticles?.Items ?? new List<ContentPost>();
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
    // protected override async Task OnParametersSetAsync()
    // {
    //     postBreadcrumbItem = new()
    //     {
    //         Label = slug ?? "Not Found",
    //         Url = $"/content/community/post/detail/{slug}",
    //         IsLast = true
    //     };
    //     postBreadcrumbCategory = new()
    //     {
    //         Label = slug ?? "Not Found",
    //         //Url = "/content/community",
    //         Url = $"/content/community"
    //     };

    //     breadcrumbItems = new List<QLN.Web.Shared.Components.BreadCrumb.BreadcrumbItem>
    //     {
    //         new() { Label = "Community", Url = "/content/community" },
    //        postBreadcrumbCategory,
    //         postBreadcrumbItem
    //     };
    // }

    protected async Task<ContentPost> GetNewsBySlugAsync()
    {
        try
        {
            var apiResponse = await _newsService.GetNewsBySlugAsync(slug) ?? new HttpResponseMessage();
            if (apiResponse.IsSuccessStatusCode && apiResponse.Content != null)
            {
                var response = await apiResponse.Content.ReadFromJsonAsync<ContentPost>();
                return response ?? new ContentPost();
            }
            return new ContentPost();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "GetNewsBySlugAsync");
            return new ContentPost();
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
    private async Task LoadBanners()
    {
        isLoadingBanners = true;
        try
        {
            var banners = await _simpleCacheService.GetBannerAsync();
            DailyHeroBanners = banners?.ContentArticleHero ?? new List<BannerItem>();
            ArticleSideBanners = banners?.ContentArticleSide ?? new List<BannerItem>();
        }
        finally
        {
            isLoadingBanners = false;
        }
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
    //    private async Task<BannerResponse?> FetchBannerData()
    //{
    //try
    //{
    //    var result = await _eventService.GetBannerAsync();
    //    if (result.IsSuccessStatusCode && result.Content != null)
    //    {
    //        return await result.Content.ReadFromJsonAsync<BannerResponse>();
    //    }
    //    return null;
    //}
    //catch (Exception ex)
    //{
    //    Logger.LogError(ex, "FetchBannerData error.");
    //    return null;
    //}
    //}
}

