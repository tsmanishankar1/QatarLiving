using Microsoft.AspNetCore.Components;
using QLN.Web.Shared.Services;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Components.ViewToggleButtons;
using MudBlazor;
namespace QLN.Web.Shared.Pages.Classifieds.Items.Components;

public class SearchSectionBase : ComponentBase
{
    [Inject] protected SearchStateService SearchState { get; set; }
    [Inject] protected ISnackbar Snackbar { get; set; }

    protected long? SelectedMin;
    protected long? SelectedMax;
    protected string _searchText;
    protected string _category;
    protected string _brand;
    protected bool _isSearchFocused = false;
    protected bool _isSearching = false;

    [Parameter] public EventCallback<string> OnViewModeChanged { get; set; }
    [Parameter] public EventCallback OnSearchStarted { get; set; }
    [Parameter] public EventCallback<List<PromotedItem>> OnSearchCompleted { get; set; }

    protected override async Task OnInitializedAsync()
    {
        _searchText = SearchState.SearchText;
        _category = SearchState.Category;
        _brand = SearchState.Brand;
        _selectedView = SearchState.ViewMode;

        if (SearchState.Results == null || !SearchState.Results.Any())
        {
            await PerformSearch();
        }
    }

    protected async Task ApplyPriceFilter()
    {
        SearchState.MinPrice = SelectedMin;
        SearchState.MaxPrice = SelectedMax;

        await PerformSearch();
    }

    protected async Task ResetPriceFilter()
    {
        SelectedMin = null;
        SelectedMax = null;
        SearchState.MinPrice = null;
        SearchState.MaxPrice = null;

        await PerformSearch();
    }

    protected async Task OnFilterChanged(string field, string value)
    {
        if (field == nameof(_category))
        {
            _category = value;
            _brand = null;
        }
        else if (field == nameof(_brand))
        {
            _brand = value;
        }

        await PerformSearch();
        StateHasChanged();
    }

    protected async Task PerformSearch()
    {
        // Actual logic goes here. Right now, placeholder:
        _isSearching = true;
        await Task.Delay(500); // Simulate loading

        // Optionally trigger events
        await OnSearchStarted.InvokeAsync();
        SearchState.SearchText = _searchText;
        SearchState.Category = _category;
        SearchState.Brand = _brand;
        SearchState.ViewMode = _selectedView;

        // TODO: Your actual search logic to fetch results
        var results = new List<PromotedItem>(); // Populate this with actual data
        SearchState.Results = results;

        _isSearching = false;
        await OnSearchCompleted.InvokeAsync(results);
    }

    protected void ClearSearch() => _searchText = string.Empty;

    protected string _selectedView = "grid";
    protected void SetViewMode(string view)
    {
        _selectedView = view;
        OnViewModeChanged.InvokeAsync(view);
    }

    protected List<ViewToggleButtons.ViewToggleOption> _viewOptions = new()
    {
        new() { ImageUrl = "/qln-images/list_icon.svg", Label = "List", Value = "list" },
        new() { ImageUrl = "/qln-images/grid_icon.svg", Label = "Grid", Value = "grid" }
    };

   protected List<CategoryItem> _categories = new()
{
    new() { Id = "mobiles", Label = "Mobile Phones & Tablets" },
    new() { Id = "electronics", Label = "Electronics" },
    new() { Id = "furniture", Label = "Furniture" },
};

protected List<BrandItem> _brands = new()
{
    new() { Id = "google", Label = "Google" },
    new() { Id = "apple", Label = "Apple" },
    new() { Id = "sony", Label = "Sony" },
};

// Define DTOs if not already
public class CategoryItem
{
    public string Id { get; set; }
    public string Label { get; set; }
}

public class BrandItem
{
    public string Id { get; set; }
    public string Label { get; set; }
}
}
