using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Web.Shared.Components;
namespace QLN.Web.Shared.Pages.Content.DailyV2.DailyNewsCard
{
    public class DailyNewsCardBase : QLComponentBase
    {
        [Inject] NavigationManager NavigationManager { get; set; }
        [Parameter]
        public ContentPost Item { get; set; }
        [Parameter]
        public bool IsHorizontal { get; set; } = false;

        protected void NavigateToDetailPage()
        {
            if (Item.NodeType.Contains("post") && !string.IsNullOrWhiteSpace(Item.Slug))
            {
                NavigationManager.NavigateTo($"{NavigationPath.Value.ContentNewsDailyDetails}{Item.Slug}");
            }
            else if (Item.NodeType.Contains("event") && !string.IsNullOrWhiteSpace(Item.Slug))
            {
                NavigationManager.NavigateTo($"{NavigationPath.Value.ContentEventsDetail}{Item.Slug}");
            }
        }
    }
}