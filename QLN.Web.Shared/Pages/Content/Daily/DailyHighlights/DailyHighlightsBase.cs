using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;

namespace QLN.Web.Shared.Pages.Content.Daily.DailyHighlights
{
    public class DailyHighlightsBase : ComponentBase
    {
        [Parameter]
        public string QueueLabel {  get; set; }

        [Parameter]
        public List<ContentPost> ListOfItems { get; set; }
    }
}
