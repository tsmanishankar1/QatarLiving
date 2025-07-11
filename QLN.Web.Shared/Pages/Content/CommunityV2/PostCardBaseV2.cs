using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using QLN.Web.Shared.Contracts;
using QLN.Web.Shared.Model;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Helpers;
using MudBlazor;
using Microsoft.Extensions.Hosting;
using QLN.Web.Shared.Components.ReportDialog;

namespace QLN.Web.Shared.Pages.Content.CommunityV2
{
    public class PostCardBaseV2 : ComponentBase
    {
        [Inject]
        protected NavigationManager Navigation { get; set; }
        [Inject] protected IJSRuntime JS { get; set; }
        [Inject] protected ICommunityService CommunityService { get; set; } = default!;
        [Inject] protected CookieAuthStateProvider CookieAuthenticationStateProvider { get; set; }

        [Inject] protected ISnackbar Snackbar { get; set; }

        [Inject] protected IPostInteractionService PostInteractionService { get; set; }
        [Inject] protected IDialogService DialogService { get; set; }

        [Parameter] public PostModel Post { get; set; } = new();
        [Parameter] public bool IsDetailView { get; set; } = false;

        protected bool IsLiked { get; set; } = false;
        protected bool IsDisliked { get; set; } = false;
        protected bool isMenuOpen = false;
        public bool IsLoggedIn { get; set; } = false;
        public string UID { get; set; } 

        protected void OnMenuToggle(bool open)
        {
            isMenuOpen = open;
            StateHasChanged();
        }
        protected override async Task OnInitializedAsync()
        {
            var authState = await CookieAuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (user.Identity?.IsAuthenticated == true)
            {
                IsLoggedIn = true;
                UID =user.FindFirst("uid")?.Value;
            }

        }


        protected void NavigateToPostDetail()
        {
            Navigation.NavigateTo($"/content/v2/community/post/detail/{Post.Slug}"); // needs injection of NavigationPath options and then add navigationPath.ContentCommunity as a prefix to this string.
        }
        protected async Task ToggleLikeAsync()
        {
            if (!IsLoggedIn)
            {
                Snackbar.Add("Please login to like this post.", Severity.Warning);
                return;
            }
            try
            {
                var success = await CommunityService.LikeCommunityPostAsync(Post.Id);

                if (success)
                {
                    IsLiked = !IsLiked;
                    Post.LikeCount += IsLiked ? 1 : -1;
                }
                else
                {
                    Snackbar.Add("Failed to like the post.", Severity.Warning);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while liking post: {ex.Message}");
                Snackbar.Add("An unexpected error occurred.", Severity.Error);
            }

            StateHasChanged();
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
        : $"{Navigation.BaseUri.TrimEnd('/')}/content/v2/community/post/detail/{Post.Slug}";

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
                    Snackbar.Add(builder =>
                    {
                        builder.OpenElement(0, "div");

                        builder.OpenElement(1, "h6");
                        builder.AddAttribute(2, "style", "margin:0px;color:black;font-weight:bold;");
                        builder.AddContent(3, "Item Link Copied");
                        builder.CloseElement();

                        builder.OpenElement(4, "div");
                        builder.AddAttribute(5, "style", "color:black;");
                        builder.AddContent(6, "Item link has been copied to the clipboard");
                        builder.CloseElement();

                        builder.CloseElement();
                    }, Severity.Success,c => c.SnackbarVariant = Variant.Outlined);
                }
                else
                {
                    Snackbar.Add(builder =>
                    {
                        builder.OpenElement(0, "div");

                        builder.OpenElement(1, "h6");
                        builder.AddAttribute(2, "style", "margin:0px;color:black;font-weight:bold;");
                        builder.AddContent(3, "Failed");
                        builder.CloseElement();

                        builder.OpenElement(4, "div");
                        builder.AddAttribute(5, "style", "color:black;");
                        builder.AddContent(6, "Failed to copy link. Please try again.");
                        builder.CloseElement();

                        builder.CloseElement();
                    }, Severity.Error,c => c.SnackbarVariant = Variant.Outlined);
                }
            }
        }
    };
        protected async Task OnReportClick()
        {
            isMenuOpen = true;
            StateHasChanged();
        }
        protected async Task OnReport()
        {
            if (!IsLoggedIn)
            {
                Snackbar.Add("Please login to report this post.", Severity.Warning);
                return;
            }
            var parameters = new DialogParameters
    {
        { "PostId", Post.Id },
        { "Type", "CommunityPost" }
    };

            var options = new DialogOptions
            {
                CloseButton = false,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = DialogService.Show<ReportDialog>("", parameters, options);
            var result = await dialog.Result;


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
