using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;
namespace QLN.Web.Shared.Pages.Content.DailyV2.DailyNewsCard
{
    public class DailyNewsCardBase : ComponentBase
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
                NavigationManager.NavigateTo($"/content/daily/article/details/{Item.Slug}");
            }
            else if (Item.NodeType.Contains("event") && !string.IsNullOrWhiteSpace(Item.Slug))
            {
                NavigationManager.NavigateTo($"/content/events/details/{Item.Slug}");
            }
        }
    }
}