using Microsoft.AspNetCore.Components;

namespace QLN.ContentBO.WebUI.Pages
{
    public partial class ServicesBase : ComponentBase
    {
        protected int ActiveIndex { get; set; } = 0;

        protected List<string> TabTitles { get; set; } = new()
        {
            "View  Subscription  Listing",
            "P2p Listings",
            "P2p Transactions",
            "Subscription Ads",
            "Verified Seller Request"
        };

          protected RenderFragment RenderTabContent() => builder =>
        {
            switch (ActiveIndex)
            {
                case 0:
                    builder.OpenComponent(0, typeof(QLN.ContentBO.WebUI.Pages.Services.ViewSubscriptionListing.ViewSubscriptionListing));
                    builder.CloseComponent();
                    break;
                case 1:
                    builder.OpenComponent(1, typeof(QLN.ContentBO.WebUI.Pages.Services.P2PListings.P2PListing));
                    builder.CloseComponent();
                    break;
                case 2:
                    builder.OpenComponent(2, typeof(QLN.ContentBO.WebUI.Pages.Services.P2PTransaction.P2PTransaction));
                    builder.CloseComponent();
                    break;
                case 3:
                    builder.OpenComponent(3, typeof(QLN.ContentBO.WebUI.Pages.Services.SubscriptionAds.SubscriptionAds));
                    builder.CloseComponent();
                    break;
                case 4:
                    builder.OpenComponent(3, typeof(QLN.ContentBO.WebUI.Pages.Services.VerifiedSellerRequest.VerifiedSellerRequest));
                    builder.CloseComponent();
                    break;
                default:
                    builder.AddContent(4, "Invalid tab");
                    break;
            }
        };

    }
}
