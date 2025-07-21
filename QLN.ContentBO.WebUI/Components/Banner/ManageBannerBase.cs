using Microsoft.EntityFrameworkCore.Metadata.Internal;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Pages.EventsPage;
using QLN.ContentBO.WebUI.Services;
using static QLN.ContentBO.WebUI.Components.ToggleTabs.ToggleTabs;

namespace QLN.ContentBO.WebUI.Components.Banner
{
    public class ManageBannerComponentBase : QLComponentBase
    {
        protected int activeIndex = 0;
        public Guid BannerId { get; set; }
        protected string selectedTab = "daily";
        protected ManageBannerTab SelectedTab => (ManageBannerTab)activeIndex;
        protected List<TabOption> tabOptions = new()
        {
            new() { Label = "Daily", Value = "daily" },
            new() { Label = "News", Value = "news" },
            new() { Label = "Events", Value = "events" },
            new() { Label = "Community", Value = "community" }
        };
        public class BannerSlot
        {
            public int Slot { get; set; }
            public string ImageUrl { get; set; }
            public string RedirectLink { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
        }
 protected List<BannerSlot> slots = new()
    {
        new BannerSlot { Slot = 1, ImageUrl = "sample-image-1.jpg", RedirectLink = "link.lik.com", StartDate = new DateTime(2025, 5, 12), EndDate = new DateTime(2025, 5, 12) },
        new BannerSlot { Slot = 2, ImageUrl = "sample-image-2.jpg", RedirectLink = "link.lik.com", StartDate = new DateTime(2025, 5, 12), EndDate = new DateTime(2025, 5, 12) },
        new BannerSlot { Slot = 3, ImageUrl = "sample-image-3.jpg", RedirectLink = "link.lik.com", StartDate = new DateTime(2025, 5, 12), EndDate = new DateTime(2025, 5, 12) }
    };

        protected void EditBanner(Guid id)
        {
            BannerId = id;
        }
        protected override async Task OnInitializedAsync()
        {
            try
            {
                await AuthorizedPage();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "OnInitializedAsync");
                throw;
            }
        }
        protected async Task OnTabChanged(string newTab)
        {
            selectedTab = newTab;
        }
        protected async Task OnTabChanged(int index)
        {
            try
            {
                activeIndex = index;

                switch (activeIndex)
                {
                    case 0:

                        StateHasChanged();
                        break;
                    case 1:

                        StateHasChanged();
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "OnTabChanged");
            }
        }

        protected void NavigateToCreateBanner()
        {
            NavManager.NavigateTo($"/manage/banner/createbanner", true);
        }
        public enum ManageBannerTab
        {
            Content = 0,
            Classifieds = 1,
        }
    }
}
