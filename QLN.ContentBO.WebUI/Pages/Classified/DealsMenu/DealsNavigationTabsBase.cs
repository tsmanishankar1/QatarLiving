using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.ContentBO.WebUI.Components;

namespace QLN.ContentBO.WebUI.Pages.Classified.DealsMenu
{

    public class DealsNavigationTabsBase : QLComponentBase
    {
        [Inject] protected NavigationManager NavigationManager { get; set; } = default!;

        protected int ActiveIndex { get; set; }

        protected readonly List<TabItem> Tabs = new()
        {
            new TabItem("View Subscription Listing", "/manage/classified/deals/subscription/listing"),
            new TabItem("View Deals Listing", "/manage/classified/deals/listing")
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
