using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using QLN.ContentBO.WebUI.Models;
using System.Linq;

namespace QLN.ContentBO.WebUI.Components.ImagePreview
{
    public class ImagePreviewBase : ComponentBase
    {
        [Parameter] public EditAdPost Item { get; set; } = new();
        [Parameter] public bool ShowImageModal { get; set; }
        [Parameter] public EventCallback CloseGallery { get; set; }

        public int SelectedImageIndex { get; set; }

        protected Dictionary<string, bool> imageLoadedMap = new();
        protected Dictionary<string, bool> imageFailedMap = new();
        protected string? currentImageUrl;
        protected int CurrentIndex = 0;

        [Inject] protected IJSRuntime JS { get; set; } = default!;

        protected override async Task OnParametersSetAsync()
        {
            if (Item?.Images?.Any() == true)
            {
                int maxIndex = Item.Images.Count - 1;
                SelectedImageIndex = Math.Clamp(SelectedImageIndex, 0, maxIndex);
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

        private bool _categoriesSwiperInitialized = false;
        protected readonly string _uniqueId = $"ImageGallery-{Guid.NewGuid()}";

        protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        try
        {
            if (!_categoriesSwiperInitialized && ShowImageModal && Item?.Images?.Any() == true)
            {
                _categoriesSwiperInitialized = true;
                await JS.InvokeVoidAsync("initializeSwiperImagePreview", DotNetObjectReference.Create(this), _uniqueId);
            }
        }
        catch (JSException jsEx)
        {
            Console.Error.WriteLine($"JavaScript interop error in OnAfterRenderAsync: {jsEx.Message}");
            // Optionally log this to a service or display a message
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unexpected error in OnAfterRenderAsync: {ex.Message}");
        }
    }


        [JSInvokable]
        public void UpdateActiveIndex(int index)
        {
            CurrentIndex = index;
            SelectedImageIndex = index;

            if (Item?.Images?.ElementAtOrDefault(index) is AdImage image)
            {
                currentImageUrl = image.Url;
            }

            StateHasChanged();
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
            if (Item?.Images?.ElementAtOrDefault(index) is not AdImage image)
                return;

            CurrentIndex = index;
            SelectedImageIndex = index;

            currentImageUrl = null;
            StateHasChanged();

            currentImageUrl = image.Url;
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
