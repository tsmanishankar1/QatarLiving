using Microsoft.AspNetCore.Components;
using QLN.Web.Shared.Components.ViewToggleButtons;
using MudBlazor;
using QLN.Common.DTO_s;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Components.Routing;
namespace QLN.Web.Shared.Pages.Classifieds.Items.Components;
public class SearchSectionBase : ComponentBase
{
    [Inject] protected SearchStateService SearchState { get; set; }
    protected bool ShowSaveSearchPopup { get; set; } = false;

    protected List<CategoryTreeDto> CategoryTrees => SearchState.ItemCategoryTrees;

    [Parameter] public EventCallback<string> OnSearch { get; set; }

    [Parameter] public EventCallback<string> OnViewModeChanged { get; set; }
    [Inject] private ILogger<SearchSectionBase> Logger { get; set; }
    [Inject] private NavigationManager Nav { get; set; }
    protected bool _isSearchFocused = false;

    protected bool _isSearching;
      protected List<ViewToggleButtons.ViewToggleOption> _viewOptions = new()
    {
        new() { ImageUrl = "/qln-images/list_icon.svg", Label = "List", Value = "list" },
        new() { ImageUrl = "/qln-images/grid_icon.svg", Label = "Grid", Value = "grid" }
    };

    protected List<BrandItem> _brands = new()
    {
        new() { Id="google", Label="Google" },
        new() { Id="apple", Label="Apple" },
        new() { Id="sony", Label="Sony" }
    };

        protected Task HandleSaveSearch()
        {
            // Implement actual save logic here — call backend or store locally
            ShowSaveSearchPopup = false;
            return Task.CompletedTask;
        }

        protected Task CloseSaveSearchPopup()
        {
            ShowSaveSearchPopup = false;
            return Task.CompletedTask;
        }

    protected override void OnInitialized()
    {
        Nav.LocationChanged += OnLocationChanged;
    }

    private void OnLocationChanged(object sender, LocationChangedEventArgs args)
    {
       var path = new Uri(args.Location).AbsolutePath.ToLowerInvariant();

    // Keep state if we're still under /qln/classifieds/items or its subpaths
    if (!path.StartsWith("/qln/classifieds/items"))
        {
            SearchState.ItemSearchText = null;
            SearchState.ItemCategory = null;
            SearchState.ItemBrand = null;
            SearchState.ItemMinPrice = null;
            SearchState.ItemMaxPrice = null;
            SearchState.ItemViewMode ??= "grid";

            StateHasChanged();
        }
    }

    protected void OnFilterChanged(string fieldName, string value)
    {
        var prop = SearchState.GetType().GetProperty(fieldName);
        prop?.SetValue(SearchState, value);
        PerformSearch();
    }
    protected async Task PerformSearch()
    {
        _isSearching = true;
        StateHasChanged();
        await Task.Yield();
        await OnSearch.InvokeAsync(SearchState.ItemSearchText);
        _isSearching = false;
    }
    protected async Task ClearSearch()
    {
        SearchState.ItemSearchText = string.Empty;
        StateHasChanged();

        if (OnSearch.HasDelegate)
        {
            await OnSearch.InvokeAsync(string.Empty); // pass empty string as the search text
        }
    }

    protected void SetViewMode(string mode)
    {
        SearchState.ItemViewMode = mode;
        OnViewModeChanged.InvokeAsync(mode);
    }
    public class BrandItem
{
    public string Id { get; set; }
    public string Label { get; set; }
}
}
