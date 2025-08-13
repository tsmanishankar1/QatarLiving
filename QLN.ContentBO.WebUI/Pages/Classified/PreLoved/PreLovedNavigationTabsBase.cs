using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Components;

namespace QLN.ContentBO.WebUI.Pages.Classified.PreLoved
{
    public class PreLovedNavigationTabsBase :QLComponentBase
    {
        [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
        protected int ActiveIndex { get; set; }

        protected readonly List<TabItem> Tabs = new()
        {
            new("View Subscription Listing", "/manage/classified/preloved/subscription/listing"),
            new("P2p Listing", "/manage/classified/preloved/p2p/listing"),
            new("P2p Transaction", "/manage/classified/preloved/p2p/transaction"),
            new("User Verification Profile", "/manage/classified/preloved/user/profile")
        };

        protected class TabItem
        {
            public string Title { get; }
            public string Url { get; }

            public TabItem(string title, string url)
            {
                Title = title;
                Url = url;
            }
        }

        protected override void OnInitialized()
        {
            ActiveIndex = GetActiveTabIndex();
        }

        protected override void OnParametersSet()
        {
            var newIndex = GetActiveTabIndex();
            if (ActiveIndex != newIndex)
                ActiveIndex = newIndex;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender)
            {
                var expectedUrl = Tabs[ActiveIndex].Url;
                var current = NavigationManager.ToBaseRelativePath(NavigationManager.Uri).ToLower();

                if (!current.StartsWith(expectedUrl.TrimStart('/').ToLower()))
                {
                    NavigationManager.NavigateTo(expectedUrl);
                }
            }
        }


        private int GetActiveTabIndex()
        {
            var currentUri = NavigationManager.ToBaseRelativePath(NavigationManager.Uri).ToLower();

            for (int i = 0; i < Tabs.Count; i++)
            {
                if (currentUri.StartsWith(Tabs[i].Url.TrimStart('/').ToLower()))
                    return i;
            }

            return 0;
        }


    }
}
