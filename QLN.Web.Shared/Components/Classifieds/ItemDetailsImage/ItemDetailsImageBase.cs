using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using QLN.Common.DTO_s;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QLN.Web.Shared.Components.Classifieds.ItemDetailsImage
{
    public class ItemDetailsImageBase : ComponentBase
    {
        [Parameter] public ClassifiedsIndex Item { get; set; } = new();

         public int SelectedImageIndex { get; set; }

        protected Dictionary<string, bool> imageLoadedMap = new();
        protected Dictionary<string, bool> imageFailedMap = new();
        protected string? currentImageUrl;
        protected int CurrentIndex = 0;

        [Inject] protected IJSRuntime JS { get; set; } = default!;

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

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await JS.InvokeVoidAsync("initializeSwiperGallery", DotNetObjectReference.Create(this));
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


        protected bool ShowEmptyCard =>
            Item?.Images == null ||
            Item.Images.Count == 0 ||
            (currentImageUrl != null && imageFailedMap.TryGetValue(currentImageUrl, out var failed) && failed);
    }
}
