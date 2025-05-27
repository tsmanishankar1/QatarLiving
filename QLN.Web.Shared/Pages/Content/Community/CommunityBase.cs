using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Services.Interface;


namespace QLN.Web.Shared.Pages.Content.Community
{
    public class CommunityBase : ComponentBase
    {
        protected string search = string.Empty;
        protected string sortOption = "Popular";

        [Inject] private ILogger<CommunityBase> Logger { get; set; }
        [Inject] private ICommunityService CommunityService { get; set; }

        protected List<PostModel> PostList { get; set; } = [];

        //protected List<PostModel> posts = new()
        //{
        //    new PostModel
        //    {
        //        Id = "1",
        //        Category = "Advice & Help",
        //        Title = "Anyone wanna buy 5x Seated Travis Scott Tickets?",
        //        ImageUrl = "images/content/Post1.svg",
        //        Author = "Ismat Zerin",
        //        Time = DateTime.Now.AddHours(-2),
        //        LikeCount = 3,
        //        CommentCount = 12
        //    },
        //    new PostModel
        //    {
        //        Id = "1",
        //        Category = "Visa and Permits",
        //        Title = "Family Residence Visa status stuck",
        //        BodyPreview = "Looking for some advice or similar experiences. 15 April – Applied for Family Residence Visa 16 April – Uploaded missing document and resubmitted Status remained “Under Process” for 3 weeks 5 May – Visited Duhail Immigration office but was told: “Private companies come after 2 weeks” and they didn’t allow me in 7 ...",
        //        Author = "Ismat Zerin",
        //        Time = DateTime.Now.AddHours(-2),
        //        LikeCount = 3,
        //        CommentCount = 12
        //    },
        //    new PostModel
        //    {
        //        Id = "1",
        //        Category = "Visa and Permits",
        //        Title = "Family Residence Visa status stuck",
        //        ImageUrl = "images/content/Post2.svg",
        //        BodyPreview = "Looking for some advice or similar experiences. 15 April – Applied for Family Residence Visa 16 April – Uploaded missing document and resubmitted Status remained “Under Process” for 3 weeks 5 May – Visited Duhail Immigration office but was told: “Private companies come after 2 weeks” and they didn’t allow me in 7 ...",
        //        Author = "Ismat Zerin",
        //        Time = DateTime.Now.AddHours(-2),
        //        LikeCount = 3,
        //        CommentCount = 12
        //    }
        //};

        protected async override Task OnInitializedAsync()
        {
            try
            {
                PostList = await GetPostListAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "OnInitializedAsync");
            }
        }

        protected async Task HandleSearchResults()
        {
            Console.WriteLine("Search completed.");
        }
        protected async Task<List<PostModel>> GetPostListAsync()
        {
            try
            {
                var response = await CommunityService.GetAllAsync();
                if (response != null)
                {
                    return response.ToList();
                }
                return [];
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Get Post ArticleAsync");
                return new List<PostModel>();
            }
        }

    }
}
