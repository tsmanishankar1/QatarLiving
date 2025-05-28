using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace QLN.Web.Shared.Components.FaqList
{
    public class FaqListBase : ComponentBase
    {
        [Parameter] public string Title { get; set; } = "FAQ's";
        [Parameter] public List<FAQItem> Items { get; set; } = new();

        protected void ToggleFaq(FAQItem faq)
        {
            faq.Expanded = !faq.Expanded;
        }

        public class FAQItem
        {
            public string Question { get; set; }
            public string Answer { get; set; }
            public bool Expanded { get; set; } = false;
        }
    }
}
