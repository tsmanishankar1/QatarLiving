using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;

namespace QLN.Web.Shared.Pages.Content.BackOffice.Daily.TopEventBOCard
{
    public class TopEventBOCardBase : LayoutComponentBase
    {
        [Parameter] public ContentEvent Item { get; set; } = new();

    }
}
