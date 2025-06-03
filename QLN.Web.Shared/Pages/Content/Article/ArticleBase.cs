
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using QLN.Web.Shared.Model;
using System.Globalization;
using QLN.Web.Shared.Models;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Web.Shared.Services.Interface;
using System.Net.Http.Json;

public class ArticleBase : ComponentBase
{
    [Parameter]
    public string slug { get; set; }
    public bool isLoading { get; set; } = true;
     protected bool imageLoaded = false;
 
    protected List<BannerItem> DailyHeroBanners { get; set; } = new();
    protected List<BannerItem> ArticleSideBanners { get; set; } = new();
    protected bool isLoadingBanners = true;
    public List<QLN.Web.Shared.Components.BreadCrumb.BreadcrumbItem> breadcrumbItems = new();
    protected QlnNewsNewsQatarPageResponse QatarNewsContent { get; set; } = new QlnNewsNewsQatarPageResponse();
    protected List<ContentPost> moreArticleList { get; set; } = new List<ContentPost>();
    [Inject] private ILogger<NewsCardBase> Logger { get; set; }
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
    
    [Parameter]
    public NewsItem Item { get; set; }
    protected async override Task OnInitializedAsync()
    {
        try
        {
            isLoading = true;
            var bannersTask = LoadBanners();
            await Task.WhenAll(bannersTask);
            isLoading = true;
            newsArticle = await GetNewsBySlugAsync();
            SelectedPost = new PostModel
            {
                Id = newsArticle.Nid,
                Category = "",
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
            breadcrumbItems = new()
            {
                new() {   Label = "News",Url ="/content/news" },
                new() { Label = "Sports", Url = "/content/news"},
                new() { Label = newsArticle.Title, Url = "/content/article/details/{slug}", IsLast = true },
            };
            QatarNewsContent = await GetNewsQatarAsync();
            moreArticleList = QatarNewsContent?.QlnNewsNewsQatar?.MoreArticles?.Items ?? new List<ContentPost>();
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
            var banners = await FetchBannerData();
            DailyHeroBanners = banners?.ContentArticleHero ?? new List<BannerItem>();
            ArticleSideBanners = banners?.ContentArticleSide ?? new List<BannerItem>();
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
}

