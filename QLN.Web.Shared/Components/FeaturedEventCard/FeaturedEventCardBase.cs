using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;

namespace QLN.Web.Shared.Components.FeaturedEventCard
{
    public partial class FeaturedEventCardBase : ComponentBase
    {
        protected bool imageLoaded = false;

    protected bool imageFailed = false;
    protected string? currentImageUrl;

    [Parameter] public ContentEvent Item { get; set; }
    [Parameter] public EventCallback<ContentEvent> OnClick { get; set; }

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
