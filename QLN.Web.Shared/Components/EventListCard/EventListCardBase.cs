using Microsoft.AspNetCore.Components;
using System;

namespace QLN.Web.Shared.Components.EventListCard
{
    public partial class EventListCardBase : ComponentBase
    {
        [Parameter]
        public EventItem Item { get; set; } = new();

        [Parameter]
        public EventCallback<EventItem> OnClick { get; set; }

        public class EventItem
        {
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public string Location { get; set; } = string.Empty;
            public decimal PriceMin { get; set; }
            public decimal PriceMax { get; set; }
            public string ImageUrl { get; set; } = string.Empty;
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
        }
    }
}
