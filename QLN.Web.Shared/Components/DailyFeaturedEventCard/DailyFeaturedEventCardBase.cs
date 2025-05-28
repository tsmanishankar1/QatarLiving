using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;

namespace QLN.Web.Shared.Components.DailyFeaturedEventCard
{
    public partial class DailyFeaturedEventCardBase : ComponentBase
    {
        [Parameter]
        public ContentPost Item { get; set; } = new();

        [Parameter]
        public EventCallback<ContentPost> OnClick { get; set; }
    }
}
