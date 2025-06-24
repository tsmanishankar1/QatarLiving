using Microsoft.AspNetCore.Components;
using QLN.Web.Shared.Components.ViewToggleButtons;
using MudBlazor;
using QLN.Common.DTO_s;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Components.Routing;
namespace QLN.Web.Shared.Pages.Classifieds.Preloved.Components;
public class PrelovedSearchSectionBase : ComponentBase
{
    [Inject] protected SearchStateService SearchState { get; set; }
    protected bool ShowSaveSearchPopup { get; set; } = false;

    protected List<CategoryTreeDto> CategoryTrees => SearchState.PrelovedCategoryTrees;

    [Parameter] public EventCallback<string> OnSearch { get; set; }

    [Parameter] public EventCallback<string> OnViewModeChanged { get; set; }
    [Inject] private ILogger<PrelovedSearchSectionBase> Logger { get; set; }
    [Inject] private NavigationManager Nav { get; set; }
    protected bool _isSearchFocused = false;
 [Parameter] public bool Loading { get; set; } = false;
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
    if (!path.StartsWith("/qln/classifieds/preloved"))
        {
            SearchState.PrelovedSearchText = null;
            SearchState.PrelovedCategory = null;
            SearchState.PrelovedBrand = null;
            SearchState.PrelovedMinPrice = null;
            SearchState.PrelovedMaxPrice = null;
            SearchState.PrelovedViewMode ??= "grid";

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
        StateHasChanged();
        await Task.Yield();
        await OnSearch.InvokeAsync(SearchState.PrelovedSearchText);
    }
    protected async Task ClearSearch()
    {
        SearchState.PrelovedSearchText = string.Empty;
        StateHasChanged();

        if (OnSearch.HasDelegate)
        {
            await OnSearch.InvokeAsync(string.Empty); // pass empty string as the search text
        }
    }

    protected void SetViewMode(string mode)
    {
        SearchState.PrelovedViewMode = mode;
        OnViewModeChanged.InvokeAsync(mode);
    }
    public class BrandItem
{
    public string Id { get; set; }
    public string Label { get; set; }
}
}
