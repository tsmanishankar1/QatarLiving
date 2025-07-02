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
    protected bool _isSearchFocused = false;

    [Parameter]
    public bool Loading { get; set; } = false;
      protected List<ViewToggleButtons.ViewToggleOption> _viewOptions = new()
    {
        new() { ImageUrl = "/qln-images/list_icon.svg", Label = "List", Value = "list" },
        new() { ImageUrl = "/qln-images/grid_icon.svg", Label = "Grid", Value = "grid" }
    };

    protected List<BrandItem> _brands = new()
    {
        new() { Id="Used", Label="Used" },
        new() { Id="Brand New", Label="Brand New" },
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
    SearchState.CollectiblesCategory = categoryId;
    SearchState.CollectiblesSubCategory = null;
    SearchState.CollectiblesSubSubCategory = null;
    await PerformSearch();
}
protected async Task OnSubCategorySelected(string subId)
{
    SearchState.CollectiblesSubCategory = subId;
    SearchState.CollectiblesSubSubCategory = null;
    await PerformSearch();
}

protected async Task OnSubSubCategorySelected(string subSubId)
{
    SearchState.CollectiblesSubSubCategory = subSubId;
    await PerformSearch();
}

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
    public class BrandItem
{
    public string Id { get; set; }
    public string Label { get; set; }
}
}
