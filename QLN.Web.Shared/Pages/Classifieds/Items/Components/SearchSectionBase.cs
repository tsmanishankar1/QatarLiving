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
    [SupplyParameterFromQuery(Name = "categoryIdL1")]
    public string CategoryIdL1FromQuery { get; set; }

    [SupplyParameterFromQuery(Name = "categoryIdL2")]
    public string CategoryIdL2FromQuery { get; set; }
    [Inject] protected ISnackbar Snackbar { get; set; }

    [Inject] protected SearchStateService SearchState { get; set; }
    protected bool ShowSaveSearchPopup { get; set; } = false;
    [Parameter]
    public EventCallback<string> OnCategoryChanged { get; set; }

   [Parameter]
    public EventCallback<string> SaveSearchAsync { get; set; }
    [Parameter]
    public bool IsLoadingSaveSearch { get; set; } = false;
    [Parameter]
    public bool IsSaveSearch { get; set; } = false;

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
        {
            // Exclude "Brands" field from SubCategory
            return SelectedSubCategory.Fields
                .Where(f => !f.Name?.Trim().Equals("Brands", StringComparison.OrdinalIgnoreCase) ?? true)
                .ToList();
        }

        if (SelectedCategory?.Fields?.Any() == true)
            return SelectedCategory.Fields;

        return new();
    }
}

    protected CategoryField? brandField;
    protected bool isBrandFieldAvailable;
   protected override void OnParametersSet()
    {
        // ONLY check brand field from SelectedSubCategory
        var brandFromSubCategory = SelectedSubCategory.Fields?.FirstOrDefault(f =>
            f.Name?.Trim().Equals("Brands", StringComparison.OrdinalIgnoreCase) == true &&
            f.Type?.Trim().ToLower() == "dropdown");

        var brandFromSubSubCategory = SelectedSubSubCategory.Fields?.FirstOrDefault(f =>
            f.Name?.Trim().Equals("Brands", StringComparison.OrdinalIgnoreCase) == true &&
            f.Type?.Trim().ToLower() == "dropdown");

        // Show brand ONLY if it's in subcategory and NOT in sub-subcategory
        brandField = (brandFromSubCategory != null && brandFromSubSubCategory == null) ? brandFromSubCategory : null;
        isBrandFieldAvailable = brandField?.Options?.Any() == true;
    }
    private bool _categoryHandledFromQuery = false;

    protected override async Task OnParametersSetAsync()
    {
        if (_categoryHandledFromQuery || CategoryTrees?.Any() != true)
            return;

        if (!string.IsNullOrWhiteSpace(CategoryIdFromQuery))
        {
            var matched = CategoryTrees.FirstOrDefault(c => c.Id.ToString() == CategoryIdFromQuery);
            if (matched != null)
            {
                SearchState.ItemCategory = matched.Id.ToString();
                SearchState.ItemSubCategory = null;
                SearchState.ItemSubSubCategory = null;
                _categoryHandledFromQuery = true;
                await PerformSearch();
                return;
            }
        }

        if (!string.IsNullOrWhiteSpace(CategoryIdL1FromQuery))
        {
            var category = CategoryTrees
                .FirstOrDefault(cat => cat.Children.Any(sub => sub.Id.ToString() == CategoryIdL1FromQuery));
            var subCategory = category?.Children?.FirstOrDefault(c => c.Id.ToString() == CategoryIdL1FromQuery);

            if (category != null && subCategory != null)
            {
                SearchState.ItemCategory = category.Id.ToString();
                SearchState.ItemSubCategory = subCategory.Id.ToString();
                SearchState.ItemSubSubCategory = null;
                _categoryHandledFromQuery = true;
                await PerformSearch();
                return;
            }
        }

        if (!string.IsNullOrWhiteSpace(CategoryIdL2FromQuery))
        {
            var category = CategoryTrees
                .FirstOrDefault(cat => cat.Children
                    .Any(sub => sub.Children
                        .Any(subsub => subsub.Id.ToString() == CategoryIdL2FromQuery)));

            var subCategory = category?.Children?
                .FirstOrDefault(sub => sub.Children
                    .Any(subsub => subsub.Id.ToString() == CategoryIdL2FromQuery));

            var subSubCategory = subCategory?.Children?
                .FirstOrDefault(subsub => subsub.Id.ToString() == CategoryIdL2FromQuery);

            if (category != null && subCategory != null && subSubCategory != null)
            {
                SearchState.ItemCategory = category.Id.ToString();
                SearchState.ItemSubCategory = subCategory.Id.ToString();
                SearchState.ItemSubSubCategory = subSubCategory.Id.ToString();
                _categoryHandledFromQuery = true;
                await PerformSearch();
            }
        }
    }
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
            await OnSearch.InvokeAsync(string.Empty); 
        }
    }

 protected async Task OnCategorySelected(string categoryId)
{
    var category = CategoryTrees.FirstOrDefault(c => c.Id.ToString() == categoryId);
    if (category != null)
    {
        SearchState.ItemCategory = category.Id.ToString();
        SearchState.SelectedCategoryName = category.Name;
    }

    SearchState.ItemSubCategory = null;
    SearchState.ItemSubSubCategory = null;
    SearchState.SelectedSubCategoryName = null;
    SearchState.SelectedSubSubCategoryName = null;
    SearchState.ItemBrand = null;

    await PerformSearch();
}

protected async Task OnSubCategorySelected(string subId)
{
    var subCategory = SelectedCategory.Children?.FirstOrDefault(c => c.Id.ToString() == subId);
    if (subCategory != null)
    {
        SearchState.ItemSubCategory = subCategory.Id.ToString();
        SearchState.SelectedSubCategoryName = subCategory.Name;
    }

    SearchState.ItemSubSubCategory = null;
    SearchState.SelectedSubSubCategoryName = null;
    SearchState.ItemBrand = null;

    await PerformSearch();
}

protected async Task OnSubSubCategorySelected(string subSubId)
{
    var subSubCategory = SelectedSubCategory.Children?.FirstOrDefault(c => c.Id.ToString() == subSubId);
    if (subSubCategory != null)
    {
        SearchState.ItemSubSubCategory = subSubCategory.Id.ToString();
        SearchState.SelectedSubSubCategoryName = subSubCategory.Name;
    }

    SearchState.ItemBrand = null;

    await PerformSearch();
}

    protected void SetViewMode(string mode)
    {
        SearchState.ItemViewMode = mode;
        OnViewModeChanged.InvokeAsync(mode);
    }
    protected Task HandleSecondaryClick()
{
    if (IsSaveSearch)
    {
        // Nav.NavigateTo("/qln/classifieds/saved-searches");
        ShowSaveSearchPopup = false;
    }
    else
    {
        ShowSaveSearchPopup = false;
    }

    return Task.CompletedTask;
}
    protected async Task HandlePrimaryClick(string searchName)
    {
        if (IsSaveSearch)
        {
            ShowSaveSearchPopup = false;
        }
        else
        {
            await HandleSaveSearch(searchName);
        }
    }

   protected async Task HandleSaveSearch(string searchName)
    {
        if (SaveSearchAsync.HasDelegate)
        {
            await SaveSearchAsync.InvokeAsync(searchName);
        }
    }
}
