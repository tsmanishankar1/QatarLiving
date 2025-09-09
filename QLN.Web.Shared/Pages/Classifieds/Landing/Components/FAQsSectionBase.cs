using Microsoft.AspNetCore.Components;
using QLN.Common.DTO_s;

namespace QLN.Web.Shared.Pages.Classifieds.Landing.Components
{
    public class FAQsSectionBase : ComponentBase
    {
        [Parameter] public IEnumerable<LandingBackOfficeIndex>? FaqItemsList { get; set; }
        [Parameter] public bool Loading { get; set; }
    }
}
