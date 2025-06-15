using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using QLN.Web.Shared.Contracts;
using QLN.Web.Shared.Model;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Helpers;
using MudBlazor;

namespace QLN.Web.Shared.Pages.Content.Community
{
    public class PostCardBase : ComponentBase
    {
        [Inject]
        protected NavigationManager Navigation { get; set; }
        [Inject] protected IJSRuntime JS { get; set; }

        [Inject] protected ISnackbar Snackbar { get; set; }

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

        public class MenuItem
    {
        public string Text { get; set; }
        public string ImageSrc { get; set; }
        public string Route { get; set; }
        public bool OpenInNewTab { get; set; } = false;
        public Func<Task> OnClick { get; set; }
    }

        private string CurrentUrl =>
    IsDetailView
        ? Navigation.Uri  
        : $"{Navigation.BaseUri.TrimEnd('/')}/content/community/post/detail/{Post.Slug}";

        //private string CurrentUrl => $"{Navigation.BaseUri.TrimEnd('/')}/content/community/post/detail/{Post.Slug}";
        protected List<MenuItem> shareMenuItems => new()
    {
        new MenuItem
        {
            ImageSrc = "/qln-images/facebook_share_icon.svg",
            Route = SocialShareHelper.GetFacebookUrl(CurrentUrl),
            OpenInNewTab = true
        },
        new MenuItem
        {
            ImageSrc = "/qln-images/instagram_share_icon.svg",
            Route = SocialShareHelper.GetInstagramUrl(CurrentUrl),
            OpenInNewTab = true
        },
        new MenuItem
        {
            ImageSrc = "/qln-images/whatsApp_share_icon.svg",
            Route = SocialShareHelper.GetWhatsAppUrl(CurrentUrl),
            OpenInNewTab = true
        },
        new MenuItem
        {
            ImageSrc = "/qln-images/tiktok_share_icon.svg",
            Route = SocialShareHelper.GetTikTokUrl(CurrentUrl),
            OpenInNewTab = true
        },
        new MenuItem
        {
            ImageSrc = "/qln-images/x_share_icon.svg",
            Route = SocialShareHelper.GetXUrl(CurrentUrl, Post?.Title ?? ""),
            OpenInNewTab = true
        },
        new MenuItem
        {
            ImageSrc = "/qln-images/linkedin_share_icon.svg",
            Route = SocialShareHelper.GetLinkedInUrl(CurrentUrl, Post?.Title ?? ""),
            OpenInNewTab = true
        },
        new MenuItem
        {
            ImageSrc = "/qln-images/copy_link_icon.svg",
            OnClick = async () =>
            {
                bool copied = false;
                copied = await SocialShareHelper.CopyLinkToClipboardAsync(JS, CurrentUrl);
                if (copied)
                {
                    Snackbar.Add("Item link has been copied to the clipboard", Severity.Success);
                }
                else
                {
                    Snackbar.Add("Failed to copy link. Please try again.", Severity.Error);
                }
            }
        }
    };
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
