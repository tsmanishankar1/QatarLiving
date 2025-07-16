using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Pages.EventsPage;
using QLN.ContentBO.WebUI.Services;

namespace QLN.ContentBO.WebUI.Components.Banner
{
    public class ManageBannerBase : QLComponentBase
    {
        protected int activeIndex = 0;
        protected ManageBannerTab SelectedTab => (ManageBannerTab)activeIndex;

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


        public enum ManageBannerTab
        {
            Content = 0,
            Classifieds = 1,
        }
    }
}
