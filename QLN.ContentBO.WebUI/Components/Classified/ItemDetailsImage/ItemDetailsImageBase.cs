using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using QLN.ContentBO.WebUI.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MudBlazor;
using System.Linq;

namespace QLN.ContentBO.WebUI.Components.Classified.ItemDetailsImage
{
    public class ItemDetailsImageBase : ComponentBase, IDisposable
    {
        [Parameter] public PreviewAdDto Item { get; set; } = default!;
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
           if (!_categoriesSwiperInitialized && Item?.Images?.Any() == true)
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

            if (Item?.Images?.ElementAtOrDefault(index) is AdImage selected)
            {
                currentImageUrl = selected.Url;
            }

            StateHasChanged();
        }

        protected override void OnParametersSet()
        {
            if (Item?.Images?.Any() == true)
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

        protected void ShowGallery() => ShowImageModal = true;

        protected void CloseGallery() => ShowImageModal = false;

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
            if (Item?.Images?.ElementAtOrDefault(index) is not AdImage selected)
                return;

            CurrentIndex = index;
            SelectedImageIndex = index;

            currentImageUrl = null;
            StateHasChanged();

            currentImageUrl = selected.Url;
            JS.InvokeVoidAsync("goToSlide", index);
            StateHasChanged();
        }

        protected bool ShowEmptyCard =>
            Item?.Images == null ||
            !Item.Images.Any() ||
            (currentImageUrl != null &&
             imageFailedMap.TryGetValue(currentImageUrl, out var failed) && failed);
    }
}
