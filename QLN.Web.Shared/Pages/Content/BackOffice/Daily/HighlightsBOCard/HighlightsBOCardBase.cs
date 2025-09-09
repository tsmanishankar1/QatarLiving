using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;

namespace QLN.Web.Shared.Pages.Content.BackOffice.Daily.HighlightsBOCard
{
    public class HighlightsBOCardBase : ComponentBase
    {
        [Inject] NavigationManager NavigationManager { get; set; }
        [Parameter]
        public string QueueLabel { get; set; }

        [Parameter]
        public List<ContentEvent> ListOfItems { get; set; }

        protected void NavigateToDetailPage(ContentEvent item)
        {
            if (item.NodeType.Contains("post") && !string.IsNullOrWhiteSpace(item.Slug))
            {
                NavigationManager.NavigateTo($"/content/article/details/{item.Slug}");
            }
            else if (item.NodeType.Contains("event") && !string.IsNullOrWhiteSpace(item.Slug))
            {
                NavigationManager.NavigateTo($"/content/events/details/{item.Slug}");
            }
        }
    }
}
