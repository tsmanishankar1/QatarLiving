using Microsoft.AspNetCore.Components;

namespace QLN.Web.Shared.Components.SubscriptionCard
{
    public class SubscriptionCardBase : ComponentBase
    {
        [Inject] protected NavigationManager Navigation { get; set; } = default!;

        [Parameter] public string Heading { get; set; } = string.Empty;
        [Parameter] public string Description { get; set; } = string.Empty;
        [Parameter] public string ButtonText { get; set; } = string.Empty;
        [Parameter] public string? ImageSrc { get; set; }
        [Parameter] public EventCallback OnClick { get; set; }

        protected bool imageLoaded = false;
        protected bool imageFailed = false;
        protected string? currentImageUrl;

        protected override void OnParametersSet()
        {
            if (currentImageUrl != ImageSrc)
            {
                currentImageUrl = ImageSrc;
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
            string.IsNullOrWhiteSpace(ImageSrc) || imageFailed;

        protected async Task HandleClick()
        {
            if (OnClick.HasDelegate)
                await OnClick.InvokeAsync();
            else
                Navigation.NavigateTo("/subscription-details");
        }
    }
}
