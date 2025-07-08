using Microsoft.AspNetCore.Components;
using QLN.Common.DTO_s;

namespace QLN.Web.Shared.Components.Classifieds.FeaturedCategoryCard
{
    public class FeaturedCategoryCardBase : ComponentBase
    {
        [Parameter]
        public LandingBackOfficeIndex Item { get; set; } = new();

        [Parameter]
        public EventCallback<LandingBackOfficeIndex> OnClick { get; set; }
        protected bool imageLoaded = false;

        protected bool imageFailed = false;
        protected string? currentImageUrl;
        protected override void OnParametersSet()
        {
            // Detect change of image and reset loading states
            if (currentImageUrl != Item.ImageUrl)
            {
                currentImageUrl = Item.ImageUrl;
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
            imageLoaded = true; // stop skeleton
            imageFailed = true; // show fallback UI
            StateHasChanged();
        }

        protected bool ShowEmptyCard =>
            string.IsNullOrWhiteSpace(Item?.ImageUrl) || imageFailed;
    }
}
