using Microsoft.AspNetCore.Components;
using QLN.Common.DTO_s;

namespace QLN.Web.Shared.Components.Classifieds.SeasonalCard
{
public class SeasonalCardBase : ComponentBase
{
    [Parameter] public LandingBackOfficeIndex Item { get; set; } = default!;

    protected bool imageLoaded = false;
    protected bool imageFailed = false;
    protected string? currentImageUrl;

    protected override void OnParametersSet()
    {
        if (currentImageUrl != Item?.ImageUrl)
        {
            currentImageUrl = Item?.ImageUrl;
            imageLoaded = false;
            imageFailed = false;
        }
    }

       [Parameter]
        public EventCallback<LandingBackOfficeIndex> OnClick { get; set; }
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
        string.IsNullOrWhiteSpace(Item?.ImageUrl) || imageFailed;
}
}