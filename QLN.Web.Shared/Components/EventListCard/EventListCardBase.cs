using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;

namespace QLN.Web.Shared.Components.EventListCard
{
    public class EventListCardBase : ComponentBase
    {
        [Parameter]
        public ContentEvent Item { get; set; } = new();

        [Parameter]
        public EventCallback<ContentEvent> OnClick { get; set; }

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
