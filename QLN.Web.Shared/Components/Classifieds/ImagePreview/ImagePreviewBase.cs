using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using QLN.Common.DTO_s;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QLN.Web.Shared.Components.Classifieds.ImagePreview
{
    public class ImagePreviewBase : ComponentBase, IAsyncDisposable
    {
       [Parameter] public ClassifiedsIndex Item { get; set; } = new();
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

            await JS.InvokeVoidAsync("toggleBodyScroll", ShowImageModal);
        }


       private bool _categoriesSwiperInitialized = false;
        protected readonly string _uniqueId = $"ImageGallery-{Guid.NewGuid()}";


        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!_categoriesSwiperInitialized  && ShowImageModal  && Item?.Images != null && Item.Images.Count > 0)
            {
                   _categoriesSwiperInitialized = true;
               await JS.InvokeVoidAsync("initializeSwiperImagePreview", DotNetObjectReference.Create(this), _uniqueId);

            }
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

                // Force re-set image URL even if same as current
                currentImageUrl = null;
                StateHasChanged(); // Force clear first to trigger re-render

                currentImageUrl = newUrl;
                JS.InvokeVoidAsync("goToSlide", index); // Sync swiper manually
                StateHasChanged(); // Re-render with new URL
            }

public async ValueTask DisposeAsync()
{
    await JS.InvokeVoidAsync("toggleBodyScroll", false);
}

        protected bool ShowEmptyCard =>
            Item?.Images == null ||
            Item.Images.Count == 0 ||
            (currentImageUrl != null && imageFailedMap.TryGetValue(currentImageUrl, out var failed) && failed);
    }
}
