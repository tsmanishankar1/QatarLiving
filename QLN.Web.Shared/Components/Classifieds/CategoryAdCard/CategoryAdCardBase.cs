using Microsoft.AspNetCore.Components;
using QLN.Common.DTO_s;

namespace QLN.Web.Shared.Components.Classifieds.CategoryAdCard
{
    public class CategoryAdCardBase : ComponentBase
    {
        [Parameter]
        public LandingBackOfficeIndex Item { get; set; } = new();

        [Parameter]
        public EventCallback<LandingBackOfficeIndex> OnClick { get; set; }

        protected Task TriggerClick()
        {
            return OnClick.InvokeAsync(Item);
        }
    }
}
