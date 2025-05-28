using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using QLN.Web.Shared.Contracts;
using QLN.Web.Shared.Model;

namespace QLN.Web.Shared.Pages.Content.Community
{
    public class PostCardBase : ComponentBase
    {
        [Inject]
        protected NavigationManager Navigation { get; set; }
        [Inject] protected IJSRuntime JS { get; set; }

        [Inject] protected IPostInteractionService PostInteractionService { get; set; }

        [Parameter] public PostModel Post { get; set; } = new();
        [Parameter] public bool IsDetailView { get; set; } = false;

        protected bool IsLiked { get; set; } = false;
        protected bool IsDisliked { get; set; } = false;

        protected void OnReport()
        {
            Console.WriteLine($"Reporting post: {Post.Title}");
        }
        protected void NavigateToPostDetail()
        {
            Navigation.NavigateTo($"/content/community/post/detail/{Post.Id}");
        }
        protected async Task ToggleLikeAsync()
        {
            var success = await PostInteractionService.LikeOrUnlikeAsync(new PostInteractionRequest
            {
                PostId = Guid.Parse(Post.Id),
                IsLike = true
            });

            if (success)
            {
                IsLiked = !IsLiked;
                if (IsLiked) IsDisliked = false;

                Post.LikeCount += IsLiked ? 1 : -1;
            }
        }

        protected async Task ToggleDislikeAsync()
        {
            var success = await PostInteractionService.LikeOrUnlikeAsync(new PostInteractionRequest
            {
                PostId = Guid.Parse(Post.Id),
                IsLike = false
            });

            if (success)
            {
                IsDisliked = !IsDisliked;
                if (IsDisliked) IsLiked = false;

                Post.LikeCount += IsDisliked ? -1 : 1;
            }
        }
        protected void SharePost()
        {
            var postUrl = $"{Navigation.BaseUri.TrimEnd('/')}/content/community/post/detail/{Post.Id}";
            var request = new ShareRequest { UrlToShare = postUrl };
            var shareUrl = ShareService.GetShareUrl(request);

            JS.InvokeVoidAsync("open", shareUrl, "_blank");
        }

    }
}
