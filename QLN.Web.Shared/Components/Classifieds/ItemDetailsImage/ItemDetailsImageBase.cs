using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using QLN.Common.DTO_s;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MudBlazor;
using QLN.Web.Shared.Helpers;

namespace QLN.Web.Shared.Components.Classifieds.ItemDetailsImage
{
    public class ItemDetailsImageBase : ComponentBase
    {
        [Parameter] public ClassifiedsIndex Item { get; set; } = new();
        [Inject] protected ILogger<ItemDetailsImageBase> Logger { get; set; } = default!;

        public int SelectedImageIndex { get; set; }
        protected string? currentImageUrl;
        protected int CurrentIndex = 0;
        protected bool ShowImageModal { get; set; }
        protected Dictionary<string, bool> imageLoadedMap = new();
        protected Dictionary<string, bool> imageFailedMap = new();
        [Inject] protected NavigationManager Navigation { get; set; }
        protected string CurrentUrl => Navigation.ToAbsoluteUri(Navigation.Uri).ToString();
        [Inject] protected ISnackbar Snackbar { get; set; }
        [Inject] protected IJSRuntime JSRuntime { get; set; }
        protected int WindowWidth { get; set; }

        [Inject] protected IJSRuntime JS { get; set; } = default!;
        protected readonly string _uniqueId = $"ImageGallery-{Guid.NewGuid()}";

        private bool _categoriesSwiperInitialized = false;
private DotNetObjectReference<ItemDetailsImageBase>? _objectReference;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                 _objectReference = DotNetObjectReference.Create(this);
                await JS.InvokeVoidAsync("blazorResize.registerResizeCallback", _objectReference);
            }

            if (!_categoriesSwiperInitialized && Item?.Images != null && Item.Images.Count > 0)
            {
                _categoriesSwiperInitialized = true;
                await JS.InvokeVoidAsync("initializeSwiperGallery", DotNetObjectReference.Create(this), _uniqueId);
            }
        }
         public void Dispose()
        {
            _objectReference?.Dispose();
        }
       [JSInvokable]
        public void UpdateActiveIndex(int index)
        {
            CurrentIndex = index;
            SelectedImageIndex = index;

            if (Item?.Images != null && index >= 0 && index < Item.Images.Count)
            {
                currentImageUrl = Item.Images[index].Url;
            }

            StateHasChanged();
        }
        protected bool IsFavorited { get; set; } = false;

        protected void ToggleFavorite()
        {
            IsFavorited = !IsFavorited;
        }


        protected override void OnParametersSet()
        {
            if (Item?.Images != null && Item.Images.Count > 0)
            {
                SelectedImageIndex = Math.Clamp(SelectedImageIndex, 0, Item.Images.Count - 1);
                var newUrl = Item.Images[SelectedImageIndex].Url;

                if (currentImageUrl != newUrl)
                {
                    currentImageUrl = newUrl;
                }
            }
            else
            {
                currentImageUrl = null;
            }
        }

        // Handle window resize from JS
        [JSInvokable]
        public void UpdateWindowWidth(int width)
        {
            WindowWidth = width;
            StateHasChanged();
        }

        // Handle modal open from "View All" (desktop only)
        protected void HandleViewAllClick()
        {
            if (WindowWidth >= 770)
            {
                ShowGallery();
            }
        }

        // Handle modal open from image click (mobile only)
        protected void HandleMainImageClick()
        {
            if (WindowWidth < 770)
            {
                ShowGallery();
            }
        }

        protected void ShowGallery()
        {
            ShowImageModal = true;
        }

        protected void CloseGallery()
        {
            ShowImageModal = false;
        }

        protected void OnImageLoaded(string url)
        {
            imageLoadedMap[url] = true;
            imageFailedMap[url] = false;
            StateHasChanged();
        }

        protected void OnImageError(string url)
        {
            imageLoadedMap[url] = true;
            imageFailedMap[url] = true;
            StateHasChanged();
        }

        protected void OnThumbnailClicked(int index)
        {
            if (Item?.Images == null || index < 0 || index >= Item.Images.Count)
                return;

            CurrentIndex = index;
            SelectedImageIndex = index;

            var newUrl = Item.Images[index].Url;

            currentImageUrl = null;
            StateHasChanged();

            currentImageUrl = newUrl;
            JS.InvokeVoidAsync("goToSlide", index);
            StateHasChanged();
        }

        protected bool ShowEmptyCard =>
            Item?.Images == null ||
            Item.Images.Count == 0 ||
            (currentImageUrl != null &&
             imageFailedMap.TryGetValue(currentImageUrl, out var failed) && failed);

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

    }
}
