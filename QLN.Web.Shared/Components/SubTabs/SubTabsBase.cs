using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace QLN.Web.Shared.Components
{
    public class ReusableTabsBase : ComponentBase, IDisposable
    {
        [Inject] protected NavigationManager NavigationManager { get; set; }

        [Parameter] public List<TabItem> Tabs { get; set; }

        [Parameter] public EventCallback<TabItem> OnTabClick { get; set; }

        protected override void OnInitialized()
        {
            NavigationManager.LocationChanged += HandleLocationChanged;
            UpdateActiveTab(NavigationManager.Uri);
        }

        private void HandleLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            UpdateActiveTab(e.Location);
            InvokeAsync(StateHasChanged);
        }

      protected void UpdateActiveTab(string currentUri)
    {
        var currentPath = new Uri(currentUri).AbsolutePath.ToLower();

        foreach (var item in Tabs)
        {
            var tabPath = new Uri(NavigationManager.BaseUri + item.Route.TrimStart('/')).AbsolutePath.ToLower();
            item.IsActive = currentPath.StartsWith(tabPath);
        }
    }



        protected async Task OnTabSelected(TabItem selected)
        {
            if (OnTabClick.HasDelegate)
                await OnTabClick.InvokeAsync(selected);

            NavigationManager.NavigateTo(selected.Route);
        }

        public void Dispose()
        {
            NavigationManager.LocationChanged -= HandleLocationChanged;
        }

        public class TabItem
        {
            public string Text { get; set; }
            public string Route { get; set; }
            public string ImagePath { get; set; }
            public bool IsActive { get; set; }
        }
    }
}
