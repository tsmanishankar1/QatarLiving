using Microsoft.AspNetCore.Components;
using QLN.Web.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Web.Shared.Pages.Content.Community
{
    public class PostDetailBase : ComponentBase
    {
        [Parameter]
        public string PostId { get; set; }
        protected List<QLN.Web.Shared.Components.BreadCrumb.BreadcrumbItem> breadcrumbItems = new();
        protected string search = string.Empty;
        protected string sortOption = "Popular";
        protected List<PostModel> posts = new()
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

        protected override async void OnInitialized()
        {
            breadcrumbItems = new()
        {
            new() { Label = "Community", Url = "/content/community" },
            new() { Label = "Discussion", Url = "/post-detail/{PostId:int}",IsLast=true }
        };

        }
    }
}
