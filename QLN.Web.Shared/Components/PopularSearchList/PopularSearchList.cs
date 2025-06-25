using Microsoft.AspNetCore.Components;
using QLN.Common.DTO_s;

namespace QLN.Web.Shared.Components.PopularSearchList
{
    public partial class PopularSearchList : ComponentBase
    {
        [Parameter] public string Title { get; set; } = "Popular Searches";
        [Parameter] public List<PopularSearchDto> Items { get; set; } = new();

        protected void Toggle(PopularSearchDto item)
        {
            item.IsExpanded = !item.IsExpanded;
        }
    }
}
