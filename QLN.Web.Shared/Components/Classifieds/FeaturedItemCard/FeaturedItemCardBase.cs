using Microsoft.AspNetCore.Components;
using MudBlazor;
using Microsoft.JSInterop;
using QLN.Common.DTO_s;
using System.Linq;
using System.Threading.Tasks;

namespace QLN.Web.Shared.Components.Classifieds.FeaturedItemCard
{
    public class FeaturedItemCardBase : ComponentBase
    {
        [Inject] protected IJSRuntime JS { get; set; } = default!;

        [Parameter]
        public LandingFeaturedItemDto Item { get; set; } = new();

        [Parameter]
        public EventCallback<LandingFeaturedItemDto> OnSelect { get; set; }

        [Parameter]
        public EventCallback<LandingFeaturedItemDto> OnHeartClick { get; set; }

        [Parameter]
        public bool IsFavorite { get; set; }

        protected string HeartIconClass => IsFavorite ? "heart-icon filled" : "heart-icon outlined";

        protected async Task HandleHeartClick(LandingFeaturedItemDto item)
        {
            IsFavorite = !IsFavorite;
            await OnHeartClick.InvokeAsync(item);
        }

        protected bool imageLoaded = false;
        protected bool imageFailed = false;
        protected string? currentImageUrl;

        protected string? FirstImageUrl => 
            Item.ImageURLs?.FirstOrDefault(u => !string.IsNullOrWhiteSpace(u?.Url))?.Url;

        protected override void OnParametersSet()
        {
            var newUrl = FirstImageUrl;

            if (currentImageUrl != newUrl)
            {
                currentImageUrl = newUrl;
                imageLoaded = false;
                imageFailed = false;
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
            string.IsNullOrWhiteSpace(FirstImageUrl) || imageFailed;
    }
}
