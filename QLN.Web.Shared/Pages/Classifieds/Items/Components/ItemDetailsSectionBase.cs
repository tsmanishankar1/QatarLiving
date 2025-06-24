using Microsoft.AspNetCore.Components;
using QLN.Common.DTO_s;
using QLN.Web.Shared.Components.BreadCrumb;
using MudBlazor;
using QLN.Web.Shared.Helpers;
using Microsoft.JSInterop;

namespace QLN.Web.Shared.Pages.Classifieds.Items.Components
{
    public class ItemDetailsSectionBase : ComponentBase
    {
        protected bool isSaved = false;
        public List<QLN.Web.Shared.Components.BreadCrumb.BreadcrumbItem> breadcrumbItems = new();
        
        [Inject] protected NavigationManager Navigation { get; set; }
        [Inject] protected ISnackbar Snackbar { get; set; }
        [Inject] protected IJSRuntime JSRuntime { get; set; }
        protected int _startIndex = 0;

        [Parameter]
        public ClassifiedsIndex Item { get; set; } = new();

        [Parameter]
        public List<ClassifiedsIndex> Simler { get; set; } = new();

        protected string CurrentUrl => Navigation.ToAbsoluteUri(Navigation.Uri).ToString();
        protected int selectedImageIndex = 0;
        protected string categorySegment = "items"; 
        protected void OnClickCardItem(ClassifiedsIndex item)
        {
            Navigation.NavigateTo($"/qln/classifieds/items/details/{item.Id}",true);
        }

        protected override void OnParametersSet()
        {
            if (Item == null)
            {
                throw new InvalidOperationException("Item is null. The component cannot render.");
            }
        }

        protected override void OnInitialized()
        {
           breadcrumbItems = new()
            {
                new QLN.Web.Shared.Components.BreadCrumb.BreadcrumbItem { Label = "Classifieds", Url = "/qln/classifieds" },
                new QLN.Web.Shared.Components.BreadCrumb.BreadcrumbItem { Label = "Items", Url = "/qln/classifieds/items" },
                new QLN.Web.Shared.Components.BreadCrumb.BreadcrumbItem { Label = Item?.Title ?? "Details", Url = "/qln/classifieds/items/details", IsLast = true }
            };

        }
 
        protected List<MenuItem> ShareMenuItems => new()
        {
            new MenuItem {
                Text = "Facebook",
                ImageSrc = "/qln-images/facebook_share_icon.svg",
                Route = SocialShareHelper.GetFacebookUrl(CurrentUrl),
                OpenInNewTab = true
            },
            new MenuItem {
                Text = "Instagram",
                ImageSrc = "/qln-images/instagram_share_icon.svg",
                Route = SocialShareHelper.GetInstagramUrl(CurrentUrl),
                OpenInNewTab = true
            },
            new MenuItem {
                Text = "WhatsApp",
                ImageSrc = "/qln-images/whatsApp_share_icon.svg",
                Route = SocialShareHelper.GetWhatsAppUrl(CurrentUrl),
                OpenInNewTab = true
            },
            new MenuItem {
                Text = "TikTok",
                ImageSrc = "/qln-images/tiktok_share_icon.svg",
                Route = SocialShareHelper.GetTikTokUrl(CurrentUrl),
                OpenInNewTab = true
            },
            new MenuItem {
                Text = "X (Twitter)",
                ImageSrc = "/qln-images/x_share_icon.svg",
                Route = SocialShareHelper.GetXUrl(CurrentUrl, Item?.Title ?? ""),
                OpenInNewTab = true
            },
            new MenuItem {
                Text = "LinkedIn",
                ImageSrc = "/qln-images/linkedin_share_icon.svg",
                Route = SocialShareHelper.GetLinkedInUrl(CurrentUrl, Item?.Title ?? "", Item?.Description ?? ""),
                OpenInNewTab = true
            },
            new MenuItem {
                Text = "Copy Link",
                ImageSrc = "/qln-images/copy_link_icon.svg",
                OnClick = async () =>
                {
                    var copied = await SocialShareHelper.CopyLinkToClipboardAsync(JSRuntime, CurrentUrl);
                    if (copied)
                        Snackbar.Add("Item link has been copied to the clipboard", Severity.Success);
                    else
                        Snackbar.Add("Failed to copy link. Please try again.", Severity.Error);
                }
            }
        };

        public class MenuItem
        {
            public string Text { get; set; }
            public string ImageSrc { get; set; }
            public string Route { get; set; }
            public bool OpenInNewTab { get; set; } = false;
            public Func<Task> OnClick { get; set; }
        }


        protected void OnSaveClicked()
        {
            isSaved = !isSaved;
        }

        protected string HeartIcon => isSaved
            ? "qln-images/classifieds/liked_heart.svg"
            : "qln-images/classifieds/heart.svg";
    }
}
