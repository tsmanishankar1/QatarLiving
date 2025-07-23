using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Components.ToggleTabs;
using QLN.ContentBO.WebUI.Models;

namespace QLN.ContentBO.WebUI.Pages.Classified.Collectibles.UserVerificationProfile
{
    public partial class UserVerificationProfileBase : ComponentBase
    {
       protected string selectedTab = "verificationrequests";
       protected List<ToggleTabs.TabOption> tabOptions = new()
        {
            new() { Label = "Verification Requests", Value = "verificationrequests" },
            new() { Label = "Rejected", Value = "rejected" },
            new() { Label = "Approved", Value = "approved" },
        };
         protected async Task OnTabChanged(string newTab)
        {
            selectedTab = newTab;

            int? status = newTab switch
            {
                "verificationrequests" => 1,
                "rejected" => 2,
                "approved" => 3,
                _ => null
            };

        }

    }
}
