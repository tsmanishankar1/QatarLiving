using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;

namespace QLN.Web.Shared.Pages.Content.Daily
{
    public class TopStoryCardBase : LayoutComponentBase
    {
        [Inject] NavigationManager NavigationManager { get; set; }

        [Parameter]
        public ContentPost Item { get; set; } = new();

        protected void NavigateToEventDetail()
        {
            NavigationManager.NavigateTo($"/events/details/{Item.Slug}");
        }
    }
}
