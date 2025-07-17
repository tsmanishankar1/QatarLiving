using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.ContentBO.WebUI.Components;

namespace QLN.ContentBO.WebUI.Pages.Classified
{
    public class PreLovedBase : QLComponentBase
    {
        protected int ActiveIndex { get; set; } = 0;

        protected List<string> TabTitles { get; set; } = new()
        {
            "View Subscription Listing",
            "P2p Listing",
            "P2p Transaction",
            "User Verification Profile"
        };
        protected RenderFragment RenderTabContent() => builder =>
        {
            switch (ActiveIndex)
            {
                case 0:
                    builder.OpenComponent(0, typeof(QLN.ContentBO.WebUI.Pages.Classified.PreLoved.Subscription.SubscriptionListing));
                    builder.CloseComponent();
                    break;
                case 1:
                    builder.OpenComponent(1, typeof(QLN.ContentBO.WebUI.Pages.Classified.PreLoved.P2p.P2pListing));
                    builder.CloseComponent();
                    break;
                case 2:
                    builder.OpenComponent(2, typeof(QLN.ContentBO.WebUI.Pages.Classified.PreLoved.P2p.P2pListing));
                    builder.CloseComponent();
                    break;
                case 3:
                    builder.OpenComponent(3, typeof(QLN.ContentBO.WebUI.Pages.Classified.PreLoved.UserProfile.UserProfile));
                    builder.CloseComponent();
                    break;
                default:
                    builder.AddContent(4, "Invalid tab");
                    break;
            }
        };
    }
}
