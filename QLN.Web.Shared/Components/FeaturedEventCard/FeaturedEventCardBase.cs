using Microsoft.AspNetCore.Components;

namespace QLN.Web.Shared.Components.FeaturedEventCard
{
    public partial class FeaturedEventCardBase : ComponentBase
    {
        [Parameter]
        public EventItem Item { get; set; } = new();

        [Parameter]
        public EventCallback<EventItem> OnClick { get; set; }

        public class EventItem
        {
            public string Category { get; set; } = string.Empty;
            public string ImageUrl { get; set; } = string.Empty;
        }
    }
}
