using Microsoft.AspNetCore.Components;

namespace QLN.ContentBO.WebUI.Pages.Classified.Items
{
    public partial class ItemsBase : ComponentBase
    {
        protected int ActiveIndex { get; set; } = 0;

        protected List<string> TabTitles { get; set; } = new()
        {
            "View Listing",
            "View Transactions",
            "User Verification Profile",
            "Reports"
        };

          protected RenderFragment RenderTabContent() => builder =>
        {
            switch (ActiveIndex)
            {
                case 0:
                    builder.OpenComponent(0, typeof(QLN.ContentBO.WebUI.Pages.Classified.Items.ViewListing.ViewListing));
                    builder.CloseComponent();
                    break;
                case 1:
                    builder.OpenComponent(1, typeof(QLN.ContentBO.WebUI.Pages.Classified.Items.ViewTransactions.ViewTransactions));
                    builder.CloseComponent();
                    break;
                case 2:
                    builder.OpenComponent(2, typeof(QLN.ContentBO.WebUI.Pages.Classified.Items.UserVerificationProfile.UserVerificationProfile));
                    builder.CloseComponent();
                    break;
                case 3:
                    builder.OpenComponent(3, typeof(QLN.ContentBO.WebUI.Pages.Classified.Items.Reports.Reports));
                    builder.CloseComponent();
                    break;
                default:
                    builder.AddContent(4, "Invalid tab");
                    break;
            }
        };

    }
}
