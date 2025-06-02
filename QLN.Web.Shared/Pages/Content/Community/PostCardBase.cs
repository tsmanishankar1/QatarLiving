using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using QLN.Web.Shared.Contracts;
using QLN.Web.Shared.Model;
using QLN.Web.Shared.Models;

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
        protected bool isMenuOpen = false;

        protected void OnMenuToggle(bool open)
        {
            isMenuOpen = open;
            StateHasChanged();
        }
        protected void OnReport()
        {
            Console.WriteLine($"Reporting post: {Post.Title}");
        }
        protected void NavigateToPostDetail()
        {

            Navigation.NavigateTo($"/content/community/post/detail/{Post.Slug}");
        }
        protected async Task ToggleLikeAsync()
        {
            IsLiked = !IsLiked;
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
        protected async Task OnReportClick()
        {
            isMenuOpen = true;
            StateHasChanged();
        }
        protected string StripHtml(string html)
        {
            if (string.IsNullOrEmpty(html))
                return string.Empty;

            // Decode HTML entities first
            var decoded = System.Net.WebUtility.HtmlDecode(html);

            // Remove HTML tags
            var stripped = System.Text.RegularExpressions.Regex.Replace(
                decoded,
                "<[^>]*(>|$)",
                string.Empty,
                System.Text.RegularExpressions.RegexOptions.Multiline);

            return stripped;
        }
    }
}
