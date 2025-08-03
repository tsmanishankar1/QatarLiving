using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.ContentBO.WebUI.Components;
using QLN.ContentBO.WebUI.Components.ToggleTabs;
using QLN.ContentBO.WebUI.Models;

namespace QLN.ContentBO.WebUI.Pages.Classified.PreLoved.UserProfile
{
    public class UserProfileBase : QLComponentBase
    {
        protected string SearchText { get; set; } = string.Empty;

        protected string SortIcon { get; set; } = Icons.Material.Filled.Sort;

        protected DateTime? dateCreated { get; set; }
        protected DateTime? datePublished { get; set; }

        protected DateTime? tempCreatedDate { get; set; }
        protected DateTime? tempPublishedDate { get; set; }

        protected bool showCreatedPopover { get; set; } = false;
        protected bool showPublishedPopover { get; set; } = false;
        protected string SelectedTab { get; set; } = ((int)CompanyStatus.Rejected).ToString();

        protected void OnSearchChanged(ChangeEventArgs e)
        {
            SearchText = e.Value?.ToString();
        }

        protected void ToggleSort()
        {
            SortIcon = SortIcon == Icons.Material.Filled.ArrowDownward
                ? Icons.Material.Filled.ArrowUpward
                : Icons.Material.Filled.ArrowDownward;

        }
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

        protected void ToggleCreatedPopover()
        {
            showCreatedPopover = !showCreatedPopover;
        }

        protected void CancelCreatedPopover()
        {
            tempCreatedDate = dateCreated;
            showCreatedPopover = false;
        }

        protected void ConfirmCreatedPopover()
        {
            dateCreated = tempCreatedDate;
            showCreatedPopover = false;
        }

        protected void TogglePublishedPopover()
        {
            showPublishedPopover = !showPublishedPopover;
        }

        protected void CancelPublishedPopover()
        {
            tempPublishedDate = datePublished;
            showPublishedPopover = false;
        }

        protected void ConfirmPublishedPopover()
        {
            datePublished = tempPublishedDate;
            showPublishedPopover = false;
        }

        protected void ClearFilters()
        {
            dateCreated = null;
            datePublished = null;
            SearchText = string.Empty;
        }

    }
}