using Microsoft.AspNetCore.Components;

namespace QLN.ContentBO.WebUI.Pages.Classified.Stores
{
    public class StoresNavigationTabsBase : ComponentBase
    {
        [Inject] protected NavigationManager NavigationManager { get; set; } = default!;

        protected int ActiveIndex { get; set; }

        protected readonly List<TabItem> Tabs = new()
        {
            new TabItem("View  Subscription  Listing", "/manage/classified/stores/view/subscription/listing"),
            new TabItem("View Stores", "/manage/classified/stores/view/stores"),
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
            // Set initial tab index based on URL
            ActiveIndex = GetActiveTabIndex();
        }

        protected override void OnParametersSet()
        {
            // Watch for route changes
            var newIndex = GetActiveTabIndex();
            if (ActiveIndex != newIndex)
                ActiveIndex = newIndex;
        }

        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {
                // Optional: Log or perform diagnostics
            }
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
