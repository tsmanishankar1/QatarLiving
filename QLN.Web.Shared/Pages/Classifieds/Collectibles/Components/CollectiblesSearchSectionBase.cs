using Microsoft.AspNetCore.Components;
using QLN.Web.Shared.Components.ViewToggleButtons;
using MudBlazor;
using QLN.Common.DTO_s;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Components.Routing;
public class CollectiblesSearchSectionBase : ComponentBase
{
   
    public string CategoryIdFromQuery { get; set; }
    [Inject] protected SearchStateService SearchState { get; set; }
    protected bool ShowSaveSearchPopup { get; set; } = false;


    [Parameter] public EventCallback<string> OnSearch { get; set; }

    [Parameter] public EventCallback<string> OnViewModeChanged { get; set; }
    [Inject] private ILogger<CollectiblesSearchSectionBase> Logger { get; set; }
    [Inject] private NavigationManager Nav { get; set; }
    
   [Parameter]
    public EventCallback<string> SaveSearchAsync { get; set; }
    [Parameter]
    public bool IsLoadingSaveSearch { get; set; } = false;
    protected bool _isSearchFocused = false;

    [Parameter]
    public bool Loading { get; set; } = false;
    [Parameter]
    public bool IsSaveSearch { get; set; } = false;
 protected List<ViewToggleButtons.ViewToggleOption> _viewOptions = new()
    {
        new() { ImageUrl = "/qln-images/list_icon.svg", Label = "List", Value = "list" },
        new() { ImageUrl = "/qln-images/grid_icon.svg", Label = "Grid", Value = "grid" }
    };
  protected List<CategoryTreeDto> CategoryTrees => SearchState.CollectiblesCategoryTrees;
 protected CategoryTreeDto SelectedCategory =>
    CategoryTrees.FirstOrDefault(x => x.Id.ToString() == SearchState.CollectiblesCategory)
    ?? new CategoryTreeDto { Children = new List<CategoryTreeDto>(), Fields = new List<CategoryField>() };

    protected CategoryTreeDto SelectedSubCategory =>
        SelectedCategory.Children?.FirstOrDefault(x => x.Id.ToString() == SearchState.CollectiblesSubCategory)
        ?? new CategoryTreeDto { Children = new List<CategoryTreeDto>(), Fields = new List<CategoryField>() };

    protected CategoryTreeDto SelectedSubSubCategory =>
        SelectedSubCategory.Children?.FirstOrDefault(x => x.Id.ToString() == SearchState.CollectiblesSubSubCategory)
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
protected async Task OnCategorySelected(string categoryId)
{
    var category = CategoryTrees.FirstOrDefault(c => c.Id.ToString() == categoryId);
    if (category != null)
    {
        SearchState.CollectiblesCategory = category.Id.ToString();
        SearchState.SelectedCategoryName = category.Name;
    }

    SearchState.CollectiblesSubCategory = null;
    SearchState.CollectiblesSubSubCategory = null;
    SearchState.SelectedSubCategoryName = null;
    SearchState.SelectedSubSubCategoryName = null;

    await PerformSearch();
}

protected async Task OnSubCategorySelected(string subId)
{
    var subCategory = SelectedCategory.Children?.FirstOrDefault(c => c.Id.ToString() == subId);
    if (subCategory != null)
    {
        SearchState.CollectiblesSubCategory = subCategory.Id.ToString();
        SearchState.SelectedSubCategoryName = subCategory.Name;
    }

    SearchState.CollectiblesSubSubCategory = null;
    SearchState.SelectedSubSubCategoryName = null;

    await PerformSearch();
}

protected async Task OnSubSubCategorySelected(string subSubId)
{
    var subSubCategory = SelectedSubCategory.Children?.FirstOrDefault(c => c.Id.ToString() == subSubId);
    if (subSubCategory != null)
    {
        SearchState.CollectiblesSubSubCategory = subSubCategory.Id.ToString();
        SearchState.SelectedSubSubCategoryName = subSubCategory.Name;
    }

    await PerformSearch();
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
    protected override void OnInitialized()
    {
        Nav.LocationChanged += OnLocationChanged;
    }
    private void OnLocationChanged(object sender, LocationChangedEventArgs args)
    {
        var path = new Uri(args.Location).AbsolutePath.ToLowerInvariant();

        // Keep state if we're still under /qln/classifieds/items or its subpaths
        if (!path.StartsWith("/qln/classifieds/collectibles"))
        {
            SearchState.CollectiblesSearchText = null;
            SearchState.CollectiblesCategory = null;
            SearchState.CollectiblesCondition = null;
            SearchState.CollectiblesMinPrice = null;
            SearchState.CollectiblesMaxPrice = null;
            SearchState.CollectiblesSubCategory = null;
            SearchState.CollectiblesSubSubCategory = null;
            SearchState.CollectiblesViewMode ??= "grid";
            SearchState.CollectiblesFilters.Clear();
            SearchState.CollectiblesHasAuthenticityCertificate = false;
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
        await OnSearch.InvokeAsync(SearchState.CollectiblesSearchText);
    }
    protected async Task ClearSearch()
    {
        SearchState.CollectiblesSearchText = string.Empty;
        StateHasChanged();

        if (OnSearch.HasDelegate)
        {
            await OnSearch.InvokeAsync(string.Empty); // pass empty string as the search text
        }
    }

    protected void SetViewMode(string mode)
    {
        SearchState.CollectiblesViewMode = mode;
        OnViewModeChanged.InvokeAsync(mode);
    }

}
