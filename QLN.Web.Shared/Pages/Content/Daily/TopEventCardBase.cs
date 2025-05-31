using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;

namespace QLN.Web.Shared.Pages.Content.Daily
{
    public class TopEventCardBase : LayoutComponentBase
    {
        [Inject] NavigationManager NavigationManager { get; set; }
        [Parameter] public ContentEvent Item { get; set; } = new();

        protected void NavigateToEventDetail()
        {
            NavigationManager.NavigateTo($"/events/details/{Item.Slug}");
        }
    }
}
