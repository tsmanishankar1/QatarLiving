using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Services.Interface;
public class ArticleBase : ComponentBase
{
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
}
