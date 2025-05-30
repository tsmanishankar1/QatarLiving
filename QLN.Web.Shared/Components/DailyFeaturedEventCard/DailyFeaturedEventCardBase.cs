using Microsoft.AspNetCore.Components;

namespace QLN.Web.Shared.Components.DailyFeaturedEventCard
{
    public partial class DailyFeaturedEventCardBase : ComponentBase
    {
        [Parameter]
        public DailyEventItem Item { get; set; } = new();

        [Parameter]
        public EventCallback<DailyEventItem> OnClick { get; set; }

        public class DailyEventItem
        {
            public string Category { get; set; } = string.Empty;
            public string ImageUrl { get; set; } = string.Empty;
        }
    }
}
