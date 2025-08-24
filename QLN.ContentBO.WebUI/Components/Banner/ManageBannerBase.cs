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
        public class BannerSlot
        {
            public int Slot { get; set; }
            public string ImageUrl { get; set; }
            public string RedirectLink { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
        }
        protected void EditBanner(Guid id)
        {
            BannerId = id;
        }
        protected override async Task OnInitializedAsync()
        {
            try
            {
                await base.OnInitializedAsync();
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
                StateHasChanged();
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
