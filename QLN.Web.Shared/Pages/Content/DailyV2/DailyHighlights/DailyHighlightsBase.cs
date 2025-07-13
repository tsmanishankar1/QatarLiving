using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Web.Shared.Components;

namespace QLN.Web.Shared.Pages.Content.DailyV2.DailyHighlights
{
    public class DailyHighlightsBase : QLComponentBase
    {
        [Inject] NavigationManager NavigationManager { get; set; }
        [Parameter]
        public string QueueLabel {  get; set; }

        [Parameter]
        public List<ContentEvent> ListOfItems { get; set; }

        protected void NavigateToDetailPage(ContentEvent item)
        {
            if (item.NodeType.Contains("post") && !string.IsNullOrWhiteSpace(item.Slug))
            {
                NavigationManager.NavigateTo($"{NavigationPath.Value.ContentNewsDailyDetails}{item.Slug}");
            }
            else if (item.NodeType.Contains("event") && !string.IsNullOrWhiteSpace(item.Slug))
            {
                NavigationManager.NavigateTo($"{NavigationPath.Value.ContentEventsDetail}{item.Slug}");
            }
        }
    }
}
