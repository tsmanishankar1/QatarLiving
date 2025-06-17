using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using QLN.Common.DTO_s;
using System.Linq;
using System.Threading.Tasks;

namespace QLN.Web.Shared.Components.Classifieds.StoreCard
{
    public class StoreCardBase : ComponentBase
    {
        [Parameter] public LandingBackOfficeIndex StoreData { get; set; } = new();
        [Parameter] public EventCallback<LandingBackOfficeIndex> OnShopNow { get; set; }

        protected bool imageLoaded = false;
        protected bool imageFailed = false;
        protected string? currentImageUrl;

        protected override void OnParametersSet()
        {
            if (currentImageUrl != StoreData?.ImageUrl)
            {
                currentImageUrl = StoreData?.ImageUrl;
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
            string.IsNullOrWhiteSpace(StoreData?.ImageUrl) || imageFailed;
    }
}
