using Microsoft.AspNetCore.Components;
using QLN.Common.DTO_s;
using System.Collections.Generic;

namespace QLN.Web.Shared.Components.FaqList
{
    public class FaqListBase : ComponentBase
    {
        [Parameter] public List<LandingBackOfficeIndex> Items { get; set; } = new();

        protected void ToggleFaq(LandingBackOfficeIndex faq)
        {
            faq.IsExpanded = !faq.IsExpanded;
        }
    }
}
