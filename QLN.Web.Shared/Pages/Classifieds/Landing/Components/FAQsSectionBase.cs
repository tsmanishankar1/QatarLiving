using Microsoft.AspNetCore.Components;
using QLN.Common.DTO_s;
using System.Collections.Generic;

namespace QLN.Web.Shared.Pages.Classifieds.Landing.Components
{
    public class FAQsSectionBase : ComponentBase
    {
        [Parameter] public IEnumerable<LandingBackOfficeIndex>? FaqItemsList { get; set; }
        [Parameter] public bool Loading { get; set; }
    }
}
