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

    [Parameter]
    [SupplyParameterFromQuery(Name = "categoryId")]
    public string CategoryIdFromQuery { get; set; }

    [Inject] protected SearchStateService SearchState { get; set; }
    protected bool ShowSaveSearchPopup { get; set; } = false;
    [Parameter]
    public EventCallback<string> OnCategoryChanged { get; set; }

    protected List<CategoryTreeDto> CategoryTrees => SearchState.ItemCategoryTrees;
    protected CategoryTreeDto SelectedCategory =>
    CategoryTrees.FirstOrDefault(x => x.Id.ToString() == SearchState.ItemCategory)
    ?? new CategoryTreeDto { Children = new List<CategoryTreeDto>(), Fields = new List<CategoryField>() };

    protected CategoryTreeDto SelectedSubCategory =>
        SelectedCategory.Children?.FirstOrDefault(x => x.Id.ToString() == SearchState.ItemSubCategory)
        ?? new CategoryTreeDto { Children = new List<CategoryTreeDto>(), Fields = new List<CategoryField>() };

    protected CategoryTreeDto SelectedSubSubCategory =>
        SelectedSubCategory.Children?.FirstOrDefault(x => x.Id.ToString() == SearchState.ItemSubSubCategory)
        ?? new CategoryTreeDto { Children = new List<CategoryTreeDto>(), Fields = new List<CategoryField>() };

      protected List<CategoryField> SelectedFields
{
    get
    {
        if (SelectedSubSubCategory?.Fields?.Any() == true)
            return SelectedSubSubCategory.Fields;

        if (SelectedSubCategory?.Fields?.Any() == true)
            return SelectedSubCategory.Fields;

        if (SelectedCategory?.Fields?.Any() == true)
            return SelectedCategory.Fields;

        return new();
    }
}

    protected CategoryField? brandField;
    protected bool isBrandFieldAvailable;

     protected override void OnParametersSet()
    {
        if (SelectedFields is not null)
        {
            brandField = SelectedFields.FirstOrDefault(f =>
            f.Name?.Trim().Equals("Brands", StringComparison.OrdinalIgnoreCase) == true &&
            f.Type?.Trim().ToLower() == "dropdown");
            isBrandFieldAvailable = brandField?.Options?.Any() == true;
        }
        else
        {
            brandField = null;
            isBrandFieldAvailable = false;
        }
    }
    private bool _categoryHandledFromQuery = false;

    protected override async Task OnParametersSetAsync()
    {
        if (_categoryHandledFromQuery || string.IsNullOrWhiteSpace(CategoryIdFromQuery) || CategoryTrees?.Any() != true)
            return;

        var matched = CategoryTrees.FirstOrDefault(c => c.Id.ToString() == CategoryIdFromQuery);
        if (matched != null)
        {
            SearchState.ItemCategory = matched.Id.ToString();
            SearchState.ItemSubCategory = null;
            SearchState.ItemSubSubCategory = null;

            _categoryHandledFromQuery = true;
            await PerformSearch();
        }
    }

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
            SearchState.ItemSubCategory = null;
            SearchState.ItemSubSubCategory = null;
            SearchState.ItemFieldFilters.Clear();
            SearchState.ItemHasWarrantyCertificate = false;
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

   protected async Task OnCategorySelected(string categoryId)
{
    SearchState.ItemCategory = categoryId;
    SearchState.ItemSubCategory = null;
    SearchState.ItemSubSubCategory = null;
    SearchState.ItemBrand = null;
    await PerformSearch();
}
protected async Task OnSubCategorySelected(string subId)
{
    SearchState.ItemSubCategory = subId;
    SearchState.ItemSubSubCategory = null;
    SearchState.ItemBrand = null;
    await PerformSearch();
}

protected async Task OnSubSubCategorySelected(string subSubId)
{
    SearchState.ItemSubSubCategory = subSubId;
    SearchState.ItemBrand = null;
    await PerformSearch();
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
