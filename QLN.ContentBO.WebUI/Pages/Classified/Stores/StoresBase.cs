using Microsoft.AspNetCore.Components;

namespace QLN.ContentBO.WebUI.Pages.Classified.Stores
{
    public partial class StoresBase : ComponentBase
    {
        protected int ActiveIndex { get; set; } = 0;

        protected List<string> TabTitles { get; set; } = new()
        {
            "View  Subscription  Listing",
            "View Stores"
        };

          protected RenderFragment RenderTabContent() => builder =>
        {
            switch (ActiveIndex)
            {
                case 0:
                    builder.OpenComponent(0, typeof(QLN.ContentBO.WebUI.Pages.Classified.Stores.ViewSubscriptionListing.ViewSubscriptionListing));
                    builder.CloseComponent();
                    break;
                case 1:
                    builder.OpenComponent(1, typeof(QLN.ContentBO.WebUI.Pages.Classified.Stores.ViewStores.ViewStores));
                    builder.CloseComponent();
                    break;
                default:
                    builder.AddContent(4, "Invalid tab");
                    break;
            }
        };

    }
}
