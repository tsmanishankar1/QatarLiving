using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;

namespace QLN.Web.Shared.Components.FeaturedEventCard
{
    public partial class FeaturedEventCardBase : ComponentBase
    {
      [Parameter]
    public ContentPost Item { get; set; }


        [Parameter]
        public EventCallback<ContentPost> OnClick { get; set; }

    }
}
