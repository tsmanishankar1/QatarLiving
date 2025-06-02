using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;

namespace QLN.Web.Shared.Components.FeaturedEventCard
{
    public partial class FeaturedEventCardBase : ComponentBase
    {
        protected bool imageLoaded = false;

        [Parameter]
        public ContentPost Item { get; set; }

        [Parameter]
        public EventCallback<ContentPost> OnClick { get; set; }

        protected override void OnParametersSet()
        {
            imageLoaded = false; // reset loading state
        }

        protected void OnImageLoaded()
        {
            imageLoaded = true;
            StateHasChanged();
        }

        protected void OnImageError()
        {
            imageLoaded = true; // stop skeleton on error
            StateHasChanged();
        }
    }
}
