using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using QLN.Web.Shared.Model;
using QLN.Web.Shared.Models;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Web.Shared.Services.Interface;
using System.Net.Http.Json;
public class ArticleBase : ComponentBase
{
    [Parameter]
    public string slug { get; set; }
    [Inject] private ILogger<NewsCardBase> Logger { get; set; }
    [Inject] private INewsService _newsService { get; set; }
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
    protected List<PostModel> PostsList = new()
        {
        new PostModel
        {
            Category = "Advice & Help",
            Title = "Anyone wanna buy 5x Seated Travis Scott Tickets?",
            ImageUrl = "images/content/Post1.svg",
            Author = "Ismat Zerin",
            Time = DateTime.Now.AddHours(-2),
            LikeCount = 3,
            CommentCount = 12,
            isCommented=true,
            Comments = new List<CommentModel>
        {
        new CommentModel
        {
            Avatar = "images/avatars/user1.png",
            CreatedBy = "Jas",
            CreatedAt = DateTime.Now.AddMinutes(-10),
            Description = "I might be interested, can you DM the price?",
            LikeCount = 2,
            UnlikeCount = 0,
            IsByCurrentUser = true
        },
        new CommentModel
        {
            Avatar = "images/avatars/user2.png",
            CreatedBy = "Alex Morgan",
            CreatedAt = DateTime.Now.AddMinutes(-5),
            Description = "It was a great game, watched it from the stadium. Go Al Sadd 🏆🎉",
            LikeCount = 0,
            UnlikeCount = 1,
            IsByCurrentUser = false
        }
    }
     },
        };
         protected async override Task OnInitializedAsync()
        {
        try
        {
            newsArticle = await GetNewsBySlugAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "OnInitializedAsync");
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
}
