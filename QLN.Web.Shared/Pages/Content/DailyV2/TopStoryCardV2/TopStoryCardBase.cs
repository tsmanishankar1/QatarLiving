using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;

namespace QLN.Web.Shared.Pages.Content.DailyV2
{
    public class TopStoryCardBase : LayoutComponentBase
    {
        [Inject] NavigationManager NavigationManager { get; set; }

        [Parameter]
        public ContentPost Item { get; set; } = new();

        protected void NavigateToDetailPage()
        {
            if (Item.NodeType.Contains("post") && !string.IsNullOrWhiteSpace(Item.Slug))
            {
                NavigationManager.NavigateTo($"/content/daily/article/details/{Item.Slug}");
            }
            else if (Item.NodeType.Contains("event") && !string.IsNullOrWhiteSpace(Item.Slug))
            {
                NavigationManager.NavigateTo($"/content/events/details/{Item.Slug}");
            }
        }
    }
}
