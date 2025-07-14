using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Web.Shared.Components;

namespace QLN.Web.Shared.Pages.Content.DailyV2
{
    public class TopEventCardBase : QLComponentBase
    {
        [Inject] NavigationManager NavigationManager { get; set; }
        [Parameter] public ContentEvent Item { get; set; } = new();

        protected void NavigateToEventDetail()
        {
            NavigationManager.NavigateTo($"{NavigationPath.Value.ContentEventsDetail}{Item.Slug}");
        }
    }
}
