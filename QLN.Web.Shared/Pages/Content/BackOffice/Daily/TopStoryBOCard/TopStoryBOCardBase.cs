using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;

namespace QLN.Web.Shared.Pages.Content.BackOffice.Daily.TopStoryBOCard
{
    public class TopStoryBOCardBase : LayoutComponentBase
    {
        
        [Parameter]
        public bool IsMiniCard { get; set; }
    }
}
