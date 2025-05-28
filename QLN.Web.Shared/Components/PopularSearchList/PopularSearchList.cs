using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace QLN.Web.Shared.Components.PopularSearchList
{
    public partial class PopularSearchList : ComponentBase
    {
        [Parameter] public string Title { get; set; } = "Popular Searches";
        [Parameter] public List<PopularSearchItem> Items { get; set; } = new();

        protected void Toggle(PopularSearchItem item)
        {
            item.Expanded = !item.Expanded;
        }

        public class PopularSearchItem
        {
            public string Question { get; set; }
            public List<string> AnswerList { get; set; } = new();
            public bool Expanded { get; set; } = false;
        }
    }
}