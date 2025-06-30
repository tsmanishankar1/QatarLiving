using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.Common.DTO_s;
using QLN.Web.Shared.Components.Classifieds.FeaturedItemCard;
using QLN.Web.Shared.Services;
using static QLN.Web.Shared.Helpers.HttpErrorHelper;

public class SearchBarSectionBase : ComponentBase, IDisposable

{
    [Inject] protected ISnackbar Snackbar { get; set; }
    [Inject] protected SearchStateService SearchState { get; set; }
     protected List<CategoryTreeDto> CategoryTrees => SearchState.ItemCategoryTrees;

    public EventCallback<LandingFeaturedItemDto> OnSearchCompleted { get; set; }
    protected override void OnInitialized()
    {
        SearchState.OnCategoryTreesChanged += StateHasChanged;
    }
        public void Dispose()
            {
                SearchState.OnCategoryTreesChanged -= StateHasChanged;
            }

    protected string searchText ;
    protected string selectedCategory;
    protected bool loading = false;
[Inject] protected NavigationManager NavigationManager { get; set; }



protected async Task PerformSearch()
{
    if (string.IsNullOrWhiteSpace(searchText) && string.IsNullOrWhiteSpace(selectedCategory))
    {
        Snackbar.Add("Please enter search text or select a category", Severity.Warning);
        return;
    }

    loading = true;

    // 🔁 Sync to SearchState before navigating
    SearchState.ItemSearchText = searchText;
    SearchState.ItemCategory = selectedCategory;

    var uri = "/qln/classifieds/items";

    var queryParams = new List<string>();

    if (!string.IsNullOrWhiteSpace(searchText))
    {
        queryParams.Add($"searchText={Uri.EscapeDataString(searchText)}");
    }

    if (!string.IsNullOrWhiteSpace(selectedCategory))
    {
        queryParams.Add($"category={Uri.EscapeDataString(selectedCategory)}");
    }

    if (queryParams.Any())
    {
        uri += "?" + string.Join("&", queryParams);
    }

    NavigationManager.NavigateTo(uri);
    loading = false;
}

}
