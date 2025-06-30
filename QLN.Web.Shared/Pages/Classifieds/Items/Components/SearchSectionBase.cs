using Microsoft.AspNetCore.Components;
using QLN.Web.Shared.Components.ViewToggleButtons;
using MudBlazor;
using QLN.Common.DTO_s;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Components.Routing;
namespace QLN.Web.Shared.Pages.Classifieds.Items.Components;
public class SearchSectionBase : ComponentBase
{
      [Parameter]
    [SupplyParameterFromQuery]
    public string searchText { get; set; }

    [Parameter]
    [SupplyParameterFromQuery]
    public string category { get; set; }
    [Inject] protected SearchStateService SearchState { get; set; }
    protected bool ShowSaveSearchPopup { get; set; } = false;
    [Parameter]
    public EventCallback<string> OnCategoryChanged { get; set; }

    protected List<CategoryTreeDto> CategoryTrees => SearchState.ItemCategoryTrees;

    protected List<CategoryField> CategoryFilters => SearchState.ItemCategoryFilters;
    protected List<BrandItem> _brands { get; set; } = new();

    [Parameter] public EventCallback<string> OnSearch { get; set; }

    [Parameter] public EventCallback<string> OnViewModeChanged { get; set; }
    [Inject] private ILogger<SearchSectionBase> Logger { get; set; }
    [Inject] private NavigationManager Nav { get; set; }
    protected bool _isSearchFocused = false;

    [Parameter]
    public bool Loading { get; set; } = false;
      protected List<ViewToggleButtons.ViewToggleOption> _viewOptions = new()
    {
        new() { ImageUrl = "/qln-images/list_icon.svg", Label = "List", Value = "list" },
        new() { ImageUrl = "/qln-images/grid_icon.svg", Label = "Grid", Value = "grid" }
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
        StateHasChanged();
        await Task.Yield();
        await OnSearch.InvokeAsync(SearchState.ItemSearchText);
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

    protected async Task OnCategorySelected(string value)
    {
        SearchState.ItemCategory = value;

        if (OnCategoryChanged.HasDelegate)
        {
            await OnCategoryChanged.InvokeAsync(value);
        }
         UpdateBrandOptions();
        await PerformSearch(); // optional if you want search to also happen
    }
    private void UpdateBrandOptions()
    {
        var brandField = SearchState.ItemCategoryFilters
            .FirstOrDefault(f => f.Name.Equals("Brands", StringComparison.OrdinalIgnoreCase));

        if (brandField != null && brandField.Options != null)
        {
            _brands = brandField.Options
                .Select(b => new BrandItem { Id = b.ToLowerInvariant(), Label = b })
                .ToList();
        }
        else
        {
            _brands = new(); // Clear if not found
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
