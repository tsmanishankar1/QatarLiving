using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Components;

namespace QLN.ContentBO.WebUI.Pages.Classified
{
    public class DealsBase : QLComponentBase
    {
        protected int ActiveIndex { get; set; } = 0;

        protected List<string> TabTitles { get; set; } = new()
        {
            "View Subscription Listing",
            "View Deals Listing"
        };
        protected RenderFragment RenderTabContent() => builder =>
        {
            switch (ActiveIndex)
            {
                case 0:
                    builder.OpenComponent(0, typeof(QLN.ContentBO.WebUI.Pages.Classified.DealsMenu.Subscription.SubscriptionListing));
                    builder.CloseComponent();
                    break;
                case 1:
                    builder.OpenComponent(1, typeof(QLN.ContentBO.WebUI.Pages.Classified.DealsMenu.DealsSection.DealsListing));
                    builder.CloseComponent();
                    break;
             
                default:
                    builder.AddContent(4, "Invalid tab");
                    break;
            }
        };
    }
}
