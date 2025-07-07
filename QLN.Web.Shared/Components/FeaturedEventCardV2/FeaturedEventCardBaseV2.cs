using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;

namespace QLN.Web.Shared.Components.FeaturedEventCardV2
{
    public partial class FeaturedEventCardBaseV2 : ComponentBase
    {
        protected bool imageLoaded = false;

    protected bool imageFailed = false;
    protected string? currentImageUrl;

    [Parameter] public EventDTOV2 Item { get; set; }
    [Parameter] public EventCallback<EventDTOV2> OnClick { get; set; }

    protected override void OnParametersSet()
    {
        // Detect change of image and reset loading states
        if (currentImageUrl != Item.CoverImage)
        {
            currentImageUrl = Item.CoverImage;
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
        string.IsNullOrWhiteSpace(Item?.CoverImage) || imageFailed;


    }
}
