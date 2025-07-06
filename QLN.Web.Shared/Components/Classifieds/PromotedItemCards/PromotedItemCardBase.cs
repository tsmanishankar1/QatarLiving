using Microsoft.AspNetCore.Components;
using QLN.Common.DTO_s;

namespace QLN.Web.Shared.Components.Classifieds.PromotedItemCards
{
    public class PromotedItemCardBase : ComponentBase
    {
        [Parameter] public ClassifiedsIndex Item { get; set; } = new();
        [Parameter] public EventCallback<ClassifiedsIndex> OnHeartClick { get; set; }
        [Parameter] public EventCallback<ClassifiedsIndex> OnClickCard { get; set; }
        [Inject] protected NavigationManager NavigationManager { get; set; } = default!;

        protected bool ShowAuthenticated
        {
            get
            {
                var path = new Uri(NavigationManager.Uri).AbsolutePath.ToLowerInvariant();
                return path.StartsWith("/qln/classifieds/preloved") || path.StartsWith("/qln/classifieds/collectibles");
            }
        }

        protected bool isHovered = false;
        protected bool isFavorite = false;
        protected int activeIndex = 0;

        protected async Task ToggleFavorite(ClassifiedsIndex item)
        {
            isFavorite = !isFavorite;
            await OnHeartClick.InvokeAsync(item);
        }

        protected async Task HandleHeartClick(ClassifiedsIndex item)
        {
            await ToggleFavorite(item);
        }

        protected async Task HandleSelect()
        {
            if (Item != null)
            {
                await OnClickCard.InvokeAsync(Item);
            }
        }

        protected string heartIconListClass => isFavorite ? "heart-icon-fav filled" : "heart-icon-fav outlined";
        protected string heartIconClass => isFavorite ? "heart-icon filled" : "heart-icon outlined";
        protected void PrevImage()
        {
            if (Item?.Images == null || Item.Images.Count == 0)
                return;

            activeIndex = (activeIndex - 1 + Item.Images.Count) % Item.Images.Count;
        }

        protected void NextImage()
        {
            if (Item?.Images == null || Item.Images.Count == 0)
                return;

            activeIndex = (activeIndex + 1) % Item.Images.Count;
        }
        protected void SetImageIndex(int index)
        {
            activeIndex = index;
        }
        protected bool imageLoaded = false;
        protected bool imageFailed = false;
        protected string? currentImageUrl;

        protected override void OnParametersSet()
        {
            if (Item?.Images != null && Item.Images.Count > 0)
            {
                var newUrl = Item.Images[activeIndex].Url;
                if (currentImageUrl != newUrl)
                {
                    currentImageUrl = newUrl;
                    imageLoaded = false;
                    imageFailed = false;
                }
            }
        }

        protected void OnImageLoaded()
        {
            imageLoaded = true;
            imageFailed = false;
            StateHasChanged();
        }

        protected void OnImageError()
        {
            imageLoaded = true;
            imageFailed = true;
            StateHasChanged();
        }

        protected bool ShowEmptyCard =>
     Item?.Images == null || Item.Images.Count == 0 || imageFailed;


    }

}
